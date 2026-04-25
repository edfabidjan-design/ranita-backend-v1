using EcommerceApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/client")]
[Authorize(Roles = "Client")]
public class ClientController : ControllerBase
{
    private readonly AppDbContext _db;
    public ClientController(AppDbContext db) => _db = db;

    int ClientId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idStr, out var id) ? id : 0;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var id = ClientId();
        var c = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound(new { ok = false, message = "Client introuvable." });

        return Ok(new
        {
            ok = true,
            item = new
            {
                c.Id,
                FullName = c.FullName,
                c.Phone,
                c.Email,
                c.IsActive,
                c.CreatedAtUtc
            }
        });
    }

    [HttpGet("orders")]
    public async Task<IActionResult> Orders()
    {
        var id = ClientId();

        var items = await _db.Orders.AsNoTracking()
            .Where(o => o.CustomerId == id)
            .OrderByDescending(o => o.Id)
            .Select(o => new {
                o.Id,
                o.Total,
                o.Status,
                o.CreatedAt
            })
            .ToListAsync();

        return Ok(new { ok = true, items });
    }

    [HttpGet("orders/{id:int}")]
    public async Task<IActionResult> OrderDetail(int id)
    {
        var cid = ClientId();

        var order = await _db.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == cid);

        if (order == null) return NotFound(new { ok = false, message = "Commande introuvable." });

        // images produit (même logique que AdminOrdersController)
        var pids = order.Items.Select(i => i.ProductId).Distinct().ToList();
        var imagesMap = await _db.ProductImages
            .Where(pi => pids.Contains(pi.ProductId))
            .OrderByDescending(pi => pi.IsMain)
            .ThenByDescending(pi => pi.Id)
            .GroupBy(pi => pi.ProductId)
            .Select(g => new { ProductId = g.Key, Url = g.First().Url })
            .ToDictionaryAsync(x => x.ProductId, x => x.Url);

        return Ok(new
        {
            ok = true,
            item = new
            {
                order.Id,
                order.Total,
                order.Status,
                order.CreatedAt,

                // ✅ ICI : items = ...
                items = order.Items.Select(oi => new {
                    orderItemId = oi.Id,                 // ✅ OBLIGATOIRE
                    productId = oi.ProductId,
                    name = oi.ProductName,
                    qty = oi.Quantity,
                    price = oi.UnitPriceSnapshot,
                    lineTotal = oi.UnitPriceSnapshot * oi.Quantity,
                    refundedQty = oi.RefundedQuantity,
                    size = oi.SelectedSize,
                    color = oi.SelectedColor,
                    dimensions = oi.DimensionsSnapshot,
                    weightKg = oi.WeightKgSnapshot,
                    imageUrl = imagesMap.TryGetValue(oi.ProductId, out var url) ? url : null
                })
            }
        });
    }


    // =======================
    // ✅ HISTORIQUE DES RETOURS CLIENT
    // =======================

    [HttpGet("returns")]
    public async Task<IActionResult> MyReturns([FromQuery] string? status = null)
    {
        var cid = ClientId();

        var q = _db.ReturnRequests.AsNoTracking()
            .Where(r => r.CustomerId == cid);

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(r => r.Status == status);

        var items = await q
            .OrderByDescending(r => r.Id)
            .Select(r => new
            {
                r.Id,
                r.OrderId,
                r.Status,
                r.CreatedAtUtc,
                r.RequestedAt,
                ImagesCount = r.Images.Count(),
                ItemsCount = r.Items.Count(),
                r.RefundAmount,
                r.RefundMethod
            })
            .ToListAsync();

        return Ok(new { ok = true, items });
    }

    [HttpGet("returns/{id:int}")]
    public async Task<IActionResult> MyReturnDetail(int id)
    {
        var cid = ClientId();

        var r = await _db.ReturnRequests
            .AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == cid);

        if (r == null)
            return NotFound(new { ok = false, message = "Retour introuvable." });

        return Ok(new
        {
            ok = true,
            item = new
            {
                r.Id,
                r.OrderId,
                r.Status,
                r.Reason,
                r.CustomerComment,
                r.Comment,
                r.CreatedAtUtc,
                r.RequestedAt,
                r.ApprovedAt,
                r.RejectedAt,
                r.ReceivedAt,
                r.RefundedAt,
                r.ClosedAt,
                r.RefundAmount,
                r.RefundMethod,
                r.RefundReference,

                images = r.Images.Select(i => i.ImageUrl).ToList(),

                items = r.Items.Select(it => new
                {
                    it.OrderItemId,
                    it.ProductId,
                    it.VendorId,
                    qtyRequested = it.QtyRequested,
                    qtyApproved = it.QtyApproved,
                    qtyReceived = it.QtyReceived,
                    unitPrice = it.UnitPriceSnapshot,
                    refundLineAmount = it.RefundLineAmount
                }).ToList()
            }
        });
    }


    [HttpPost("orders/{id:int}/cancel")]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var cid = ClientId();
        if (cid <= 0)
            return Unauthorized(new { ok = false, message = "Client non authentifié." });

        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == cid);

        if (order == null)
            return NotFound(new { ok = false, message = "Commande introuvable." });

        var status = (order.Status ?? "").Trim().ToLowerInvariant();

        if (status != "enattente")
            return BadRequest(new { ok = false, message = "Seules les commandes en attente peuvent être annulées." });

        // ✅ ANNULATION
        order.Status = "Annulee";

        // 🔥 REMISE DU STOCK
        foreach (var item in order.Items)
        {
            var product = await _db.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == item.ProductId);

            if (product == null) continue;

            // 🔹 SI VARIANTES
            if (product.Variants != null && product.Variants.Count > 0)
            {
                var size = (item.SelectedSize ?? "").Trim().ToLower();
                var color = (item.SelectedColor ?? "").Trim().ToLower();

                var variant = product.Variants.FirstOrDefault(v =>
                    (v.Size ?? "").Trim().ToLower() == size &&
                    (v.Color ?? "").Trim().ToLower() == color
                );

                if (variant != null)
                {
                    variant.Stock += item.Quantity;
                }

                // recalcul stock total
                product.Stock = product.Variants.Sum(v => v.Stock);
            }
            else
            {
                // 🔹 PRODUIT SIMPLE
                product.Stock += item.Quantity;
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            ok = true,
            message = "Commande annulée et stock mis à jour."
        });
    }

    [HttpDelete("orders/{id:int}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var cid = ClientId();
        if (cid <= 0)
            return Unauthorized(new { ok = false, message = "Client non authentifié." });

        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == cid);

        if (order == null)
            return NotFound(new { ok = false, message = "Commande introuvable." });

        var status = (order.Status ?? "").Trim().ToLowerInvariant();

        if (status != "annulee" && status != "cancelled")
            return BadRequest(new { ok = false, message = "Seules les commandes annulées peuvent être supprimées." });

        _db.OrderItems.RemoveRange(order.Items);
        _db.Orders.Remove(order);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            ok = true,
            message = "Commande supprimée."
        });
    }

}
