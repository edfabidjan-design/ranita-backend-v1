using EcommerceApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/vendor/orders")]
[Authorize(Roles = "Vendor")]
public class VendorOrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    public VendorOrdersController(AppDbContext db) => _db = db;

    private int GetVendorId()
    {
        var s =
            User.FindFirst("vendorId")?.Value ??
            User.FindFirst("VendorId")?.Value ??
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(s, out var id) ? id : 0;
    }

    public class UpdateVendorStatusDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "";
    }

    // ✅ PUT /api/vendor/orders/{orderItemId}/status
    [HttpPut("{orderItemId:int}/status")]
    public async Task<IActionResult> UpdateVendorStatus(int orderItemId, [FromBody] UpdateVendorStatusDto dto)
    {
        var vendorId = GetVendorId();
        if (vendorId <= 0)
            return Unauthorized(new { message = "VendorId manquant dans le token." });

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { "Pending", "Confirmed", "Shipped" };

        var status = (dto?.Status ?? "").Trim();
        if (!allowed.Contains(status))
            return BadRequest(new { message = "Statut invalide.", allowed });

        var item = await _db.OrderItems
            .FirstOrDefaultAsync(x => x.Id == orderItemId && x.VendorId == vendorId);

        if (item == null)
            return NotFound(new { message = "Ligne de commande introuvable." });

        item.VendorStatus = status;
        item.IsSeenByVendor = true;
        // ✅ récupérer la commande globale
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == item.OrderId);

        if (order != null)
        {
            var items = order.Items.ToList();

            // Tous expédiés ou livrés => commande en livraison
            var allShippedOrDelivered = items.All(i =>
                string.Equals(i.VendorStatus, "Shipped", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(i.VendorStatus, "Delivered", StringComparison.OrdinalIgnoreCase));

            // Tous pending
            var allPending = items.All(i =>
                string.IsNullOrWhiteSpace(i.VendorStatus) ||
                string.Equals(i.VendorStatus, "Pending", StringComparison.OrdinalIgnoreCase));

            // Tous confirmés/pending avant expédition
            var anyConfirmed = items.Any(i =>
                string.Equals(i.VendorStatus, "Confirmed", StringComparison.OrdinalIgnoreCase));

            if (allShippedOrDelivered)
            {
                order.Status = "EnLivraison";
            }
            else if (allPending)
            {
                order.Status = "EnAttente";
            }
            else if (anyConfirmed)
            {
                order.Status = "EnPreparation";
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            id = item.Id,
            vendorStatus = item.VendorStatus,
            orderStatus = order?.Status
        });
    }

    // ✅ GET /api/vendor/orders
    [HttpGet]
    public async Task<IActionResult> GetMyOrders()
    {
        var vendorId = GetVendorId();
        if (vendorId <= 0) return Unauthorized(new { message = "VendorId manquant dans le token." });

        var items = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.VendorId == vendorId)
            .Include(oi => oi.Order)
            .Join(_db.Products.Include(p => p.Images),
                oi => oi.ProductId,
                p => p.Id,
                (oi, p) => new { oi, p })
            .OrderByDescending(x => x.oi.Id)
            .Select(x => new
            {
                id = x.oi.Id,
                orderId = x.oi.OrderId,
                createdAt = x.oi.Order != null ? (DateTime?)x.oi.Order.CreatedAt : null,

                productId = x.oi.ProductId,
                productName = x.oi.ProductName,

                productImage = x.p.Images
                    .OrderBy(i => i.SortOrder)      // enlève si pas
                    .Select(i => i.Url)             // ou i.ImageUrl
                    .FirstOrDefault(),

                quantity = x.oi.Quantity,
                unitPrice = x.oi.UnitPrice,
                vendorAmount = x.oi.VendorAmount,
                vendorStatus = x.oi.VendorStatus,

                selectedColor = x.oi.SelectedColor,
                selectedSize = x.oi.SelectedSize,

                customerName = x.oi.Order != null ? x.oi.Order.FullName : "",
                phone = x.oi.Order != null ? x.oi.Order.Phone : "",
                city = x.oi.Order != null ? x.oi.Order.City : ""
            })
            .ToListAsync();

        return Ok(new { items });
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var vendorId = GetVendorId();

        if (vendorId <= 0)
            return Unauthorized(new { message = "VendorId manquant." });

        var count = await _db.OrderItems
            .AsNoTracking()
            .CountAsync(x =>
                x.VendorId == vendorId &&
                !x.IsSeenByVendor &&
                x.VendorStatus == "Pending");

        return Ok(new { count });
    }


    [HttpPost("mark-seen")]
    public async Task<IActionResult> MarkSeen()
    {
        var vendorId = GetVendorId();

        var items = await _db.OrderItems
            .Where(x => x.VendorId == vendorId && !x.IsSeenByVendor)
            .ToListAsync();

        foreach (var i in items)
            i.IsSeenByVendor = true;

        await _db.SaveChangesAsync();

        return Ok();
    }
}