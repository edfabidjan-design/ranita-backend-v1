using EcommerceApi.Data;
using EcommerceApi.Hubs;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/admin/returns")]
[Authorize]
public class AdminReturnsController : ControllerBase
{
    private readonly IHubContext<ClientNotifHub> _hub;
    private readonly AppDbContext _db;
    private readonly EcommerceApi.Service.EmailService _email;

    public AdminReturnsController(AppDbContext db, EcommerceApi.Service.EmailService email, IHubContext<ClientNotifHub> hub)
    {
        _db = db;
        _email = email;
        _hub = hub;
    }

    // GET /api/admin/returns?status=Requested&q=134
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status = null, [FromQuery] string? q = null)
    {
        var query = _db.ReturnRequests
            .AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Order)
            .Include(r => r.Images)
            .OrderByDescending(r => r.RequestedAt) // ou CreatedAtUtc si tu veux
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("Tous", StringComparison.OrdinalIgnoreCase))
            query = query.Where(r => r.Status == status);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(r =>
                r.Id.ToString().Contains(q) ||
                r.OrderId.ToString().Contains(q) ||
                (r.Customer != null && (
                    r.Customer.FullName.Contains(q) ||
                    r.Customer.Phone.Contains(q)
                ))
            );
        }

        var items = await query.Select(r => new
        {
            r.Id,
            r.OrderId,
            r.CustomerId,
            CustomerName = r.Customer != null ? r.Customer.FullName : "",
            CustomerPhone = r.Customer != null ? r.Customer.Phone : "",
            r.Status,
            r.Reason,
            r.CustomerComment,
            r.RequestedAt,
            Images = r.Images.Select(i => i.ImageUrl).ToList()
        }).ToListAsync();

        return Ok(new { ok = true, items });
    }

    // GET /api/admin/returns/10
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var r = await _db.ReturnRequests
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Order).ThenInclude(o => o.Items)
            .Include(x => x.Items).ThenInclude(ri => ri.OrderItem)
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (r == null) return NotFound(new { ok = false, message = "Retour introuvable." });

        return Ok(new
        {
            ok = true,
            item = new
            {
                r.Id,
                r.OrderId,
                r.CustomerId,
                CustomerName = r.Customer?.FullName,
                CustomerPhone = r.Customer?.Phone,
                r.Status,
                r.Reason,
                r.CustomerComment,
                r.AdminNote,
                r.RequestedAt,
                Images = r.Images.Select(i => i.ImageUrl).ToList(),
                Items = r.Items.Select(i => new
                {
                    i.Id,
                    i.OrderItemId,
                    QtyRequested = i.QtyRequested,
                    QtyApproved = i.QtyApproved,
                    QtyReceived = i.QtyReceived,
                    ProductName = i.OrderItem != null ? i.OrderItem.ProductName : "",
                    UnitPrice = i.UnitPriceSnapshot
                }).ToList()
            }
        });
    }

    public record StatusDto(string Status);

    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] StatusDto dto)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { "Requested", "Approved", "Rejected", "Received", "Refunded", "Closed" };

        if (dto == null || string.IsNullOrWhiteSpace(dto.Status) || !allowed.Contains(dto.Status))
            return BadRequest(new { ok = false, message = "Statut invalide." });

        var r = await _db.ReturnRequests
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (r == null) return NotFound(new { ok = false, message = "Retour introuvable." });

        var adminIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(adminIdStr, out var adminId))
            r.AdminUserId = adminId;

        // ✅ 1) change status
        r.Status = dto.Status;

        // ✅ 2) timestamps
        var now = DateTime.UtcNow;
        if (dto.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase)) r.ApprovedAt = now;
        if (dto.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase)) r.RejectedAt = now;
        if (dto.Status.Equals("Received", StringComparison.OrdinalIgnoreCase)) r.ReceivedAt = now;
        if (dto.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase)) r.ClosedAt = now;

        // ✅ 3) refund UNIQUEMENT si Refunded
        if (dto.Status.Equals("Refunded", StringComparison.OrdinalIgnoreCase))
        {
            if (r.RefundedAt != null)
                return Ok(new { ok = true, message = "Déjà remboursé." });

            var items = await _db.ReturnItems
                .AsTracking()
                .Where(x => x.ReturnRequestId == r.Id)
                .ToListAsync();

            if (items.Count == 0)
                return BadRequest(new { ok = false, message = "Aucun ReturnItem pour ce retour (impossible de rembourser)." });

            foreach (var it in items)
            {
                var qty = it.QtyApproved > 0 ? it.QtyApproved : it.QtyRequested;
                if (qty <= 0) continue;

                // 🔥 RÉCUPÉRER LE ORDER ITEM
                var orderItem = await _db.OrderItems
                    .AsTracking()
                    .FirstOrDefaultAsync(x => x.Id == it.OrderItemId);

                if (orderItem != null)
                {
                    // ✅ CAS VARIANTE
                    if (orderItem.VariantId.HasValue)
                    {
                        var variant = await _db.ProductVariants
                            .FirstOrDefaultAsync(v => v.Id == orderItem.VariantId.Value);

                        if (variant != null)
                        {
                            variant.Stock += qty;
                            variant.UpdatedAt = DateTime.UtcNow;
                        }

                        var product = await _db.Products
                            .Include(p => p.Variants)
                            .FirstOrDefaultAsync(p => p.Id == orderItem.ProductId);

                        if (product != null)
                        {
                            product.Stock = product.Variants.Sum(v => v.Stock);
                            product.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        // ✅ PRODUIT SIMPLE
                        var product = await _db.Products
                            .FirstOrDefaultAsync(p => p.Id == orderItem.ProductId);

                        if (product != null)
                        {
                            product.Stock += qty;
                            product.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                // ===== TON CODE EXISTANT (NE PAS TOUCHER) =====

                var vendorDebit = it.VendorNetAmountSnapshot * qty;
                var commissionReverse = it.CommissionAmountSnapshot * qty;

                var refKey = $"REFUND:return:{r.Id}:order:{r.OrderId}:OrderItem:{it.OrderItemId}";

                var existsRefundTx = await _db.VendorWalletTransactions
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.VendorId == it.VendorId &&
                        x.Type == VendorWalletTxTypes.RefundDebit &&
                        x.Reference == refKey);

                var acc = await _db.VendorAccounts
                    .AsTracking()
                    .FirstOrDefaultAsync(a => a.VendorId == it.VendorId);

                if (acc != null)
                    acc.WalletBalance -= vendorDebit;

                if (!existsRefundTx)
                {
                    _db.VendorWalletTransactions.Add(new VendorWalletTransaction
                    {
                        VendorId = it.VendorId,
                        Amount = -vendorDebit,
                        Type = VendorWalletTxTypes.RefundDebit,
                        Reference = refKey,
                        OrderId = r.OrderId,
                        OrderItemId = it.OrderItemId,
                        ReturnRequestId = r.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                _db.AdminWalletTransactions.Add(new AdminWalletTransaction
                {
                    Amount = -commissionReverse,
                    Type = "CommissionReversal",
                    OrderId = r.OrderId,
                    OrderItemId = it.OrderItemId,
                    Note = $"REVERSAL COMMISSION return:{r.Id}"
                });

                var oi = await _db.OrderItems
                    .AsTracking()
                    .FirstOrDefaultAsync(x => x.Id == it.OrderItemId);

                if (oi != null)
                {
                    oi.RefundedQuantity += qty;
                    if (oi.RefundedQuantity > oi.Quantity)
                        oi.RefundedQuantity = oi.Quantity;

                    oi.ReturnStatus = "Refunded";
                }
            }

            r.RefundedAt = DateTime.UtcNow;
        }

        // ✅ 4) save une fois
        await _db.SaveChangesAsync();

        // ✅ 5) notification (tu peux garder ton code)
        var title = "Mise à jour de votre retour";

        var message = dto.Status.ToLowerInvariant() switch
        {
            "approved" => $"Votre demande de retour (commande #{r.OrderId}) a été acceptée. Merci de renvoyer le produit.",
            "rejected" => $"Votre demande de retour (commande #{r.OrderId}) a été refusée. Vous serez contacté par un agent.",
            "received" => $"Nous avons reçu votre produit retourné (commande #{r.OrderId}).",
            "refunded" => $"Votre remboursement pour la commande #{r.OrderId} a été effectué.",
            "closed" => $"Votre dossier de retour (commande #{r.OrderId}) est clôturé.",
            _ => $"Le statut de votre retour (commande #{r.OrderId}) a été mis à jour : {dto.Status}."
        };

        var notif = new Notification
        {
            CustomerId = r.CustomerId,
            Title = title,
            Message = message,
            Type = "Return",
            RefId = r.Id,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Notifications.Add(notif);
        await _db.SaveChangesAsync();

        await _hub.Clients.Group($"client:{r.CustomerId}")
            .SendAsync("notif:new", new
            {
                id = notif.Id,
                title = notif.Title,
                message = notif.Message,
                type = notif.Type,
                refId = notif.RefId,
                isRead = notif.IsRead,
                createdAtUtc = notif.CreatedAtUtc
            });

        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == r.CustomerId);
        if (!string.IsNullOrWhiteSpace(customer?.Email))
        {
            try { await _email.SendAsync(customer.Email, title, message); } catch { }
        }

        return Ok(new { ok = true, message = "Statut mis à jour." });
    }

    [HttpGet("pending-count")]
    public async Task<IActionResult> GetPendingCount()
    {
        var count = await _db.ReturnRequests
            .AsNoTracking()
            .CountAsync(r => r.Status == "Requested");

        return Ok(new { ok = true, count });
    }
}