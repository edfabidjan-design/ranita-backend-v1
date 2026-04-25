using EcommerceApi.Data;
using EcommerceApi.Dtos;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/returns")]
[Authorize(Roles = "Client")]
public class ReturnsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    private readonly PushService _push;

    public ReturnsController(AppDbContext db, IWebHostEnvironment env, PushService push)
    {
        _db = db;
        _env = env;
        _push = push;
    }

    [HttpPost]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Create([FromForm] CreateReturnFormDto form)
    {
        if (form == null || form.OrderId <= 0)
            return BadRequest(new { ok = false, message = "Données invalides." });

        var items = JsonSerializer.Deserialize<List<CreateReturnItemDto>>(
            form.ItemsJson ?? "[]",
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        ) ?? new List<CreateReturnItemDto>();
        if (items.Count == 0)
            return BadRequest(new { ok = false, message = "Aucun article sélectionné." });

        // ✅ IMPORTANT : vérifier que la commande appartient au client connecté
        // (adapte selon ton token: customerId claim, sub, NameIdentifier...)
        var customerIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(customerIdStr) || !int.TryParse(customerIdStr, out var customerId))
            return Unauthorized(new { ok = false, message = "Client non identifié." });

        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == form.OrderId && o.CustomerId == customerId);

        if (order == null)
            return NotFound(new { ok = false, message = "Commande introuvable." });

        if (!string.Equals(order.Status, "Livree", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { ok = false, message = "Retour possible uniquement pour commande livrée." });

        var deliveredAt = order.DeliveredAt ?? order.CreatedAt;
        if (deliveredAt < DateTime.UtcNow.AddDays(-7))
            return BadRequest(new { ok = false, message = "Délai de retour expiré (7 jours)." });

        var rr = new ReturnRequest
        {
            OrderId = order.Id,
            CustomerId = customerId,
            Status = "Requested",
            Reason = form.Reason ?? "",
            CustomerComment = form.Comment,
            Comment = form.Comment,             // si tu veux aussi remplir Comment
            RequestedAt = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

        // ✅ BLOQUER si une demande existe déjà (en cours) pour cette commande
        var hasInProgress = await _db.ReturnRequests.AnyAsync(r =>
            r.OrderId == order.Id &&
            r.CustomerId == customerId &&
            r.Status != "Rejected" &&
            r.Status != "Closed"
        );

        if (hasInProgress)
            return Conflict(new { ok = false, message = "Votre demande est en cours de traitement." });

        foreach (var itemDto in items)
        {
            var oi = order.Items.FirstOrDefault(x => x.Id == itemDto.OrderItemId);
            if (oi == null)
                return BadRequest(new { ok = false, message = $"OrderItem {itemDto.OrderItemId} invalide." });

            var remaining = oi.Quantity - oi.RefundedQuantity;
            if (itemDto.Quantity <= 0 || itemDto.Quantity > remaining)
                return BadRequest(new { ok = false, message = "Quantité retour invalide." });




            rr.Items.Add(new ReturnItem
            {
                OrderItemId = oi.Id,
                ProductId = oi.ProductId,
                VendorId = oi.VendorId,
                QtyRequested = itemDto.Quantity,

                UnitPriceSnapshot = oi.UnitPriceSnapshot,
                VendorAmountSnapshot = oi.VendorAmount,
                PlatformFeeSnapshot = oi.PlatformFee,
                CommissionRateSnapshot = oi.CommissionRateSnapshot ?? 0m,
                CommissionAmountSnapshot = oi.CommissionAmount ?? 0m,
                VendorNetAmountSnapshot = oi.VendorNetAmount ?? oi.VendorAmount
            });

            oi.ReturnStatus = "Requested";
        }

        _db.ReturnRequests.Add(rr);
        await _db.SaveChangesAsync();

        Console.WriteLine($"✅ RETURN CREATED => rrId={rr.Id}, orderId={order.Id}");
        // ✅ Notif admin (DB) + Push FCM
        try
        {
            // customer name pour la push (simple)
            var customerName = await _db.Customers.AsNoTracking()
                .Where(c => c.Id == customerId)
                .Select(c => c.FullName)
                .FirstOrDefaultAsync() ?? $"Client #{customerId}";

            _db.AdminNotifications.Add(new AdminNotification
            {
                Title = "Nouveau retour",
                Message = $"{customerName} • Commande #{order.Id} • Retour #{rr.Id}",
                Type = "Return",
                RefId = rr.Id,
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            await _push.SendNewReturnToAdminsAsync(rr.Id, order.Id, customerName);
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ PUSH RETURN ERROR: " + ex.Message);
        }

        // ✅ MULTI IMAGES
        // ✅ IMAGES (multi)
        var files = (form.Images ?? new List<IFormFile>())
            .Where(f => f != null && f.Length > 0)
            .Take(4)
            .ToList();

        if (files.Any())
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "returns");
            Directory.CreateDirectory(uploadsDir);

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file.FileName);
                var fileName = $"return_{rr.Id}_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);

                await using var stream = System.IO.File.Create(filePath);
                await file.CopyToAsync(stream);

                _db.ReturnImages.Add(new ReturnImage
                {
                    ReturnRequestId = rr.Id,
                    ImageUrl = "/uploads/returns/" + fileName
                });
            }

            await _db.SaveChangesAsync();
        }
        return Ok(new { ok = true, message = "Demande de retour envoyée.", returnId = rr.Id });
    }
    public class CreateReturnDto
    {
        public int OrderId { get; set; }
        public string? Reason { get; set; }
        public string? Comment { get; set; }
        public List<CreateReturnItemDto> Items { get; set; } = new();
    }

    public class CreateReturnItemDto
    {
        public int OrderItemId { get; set; }
        public int Quantity { get; set; }
    }
}