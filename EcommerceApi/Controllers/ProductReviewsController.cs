using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/products/{productId:int}/reviews")]
public class ProductReviewsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProductReviewsController(AppDbContext db) => _db = db;

    private int CustomerId
    {
        get
        {
            var s =
                User.FindFirstValue("customerId") ??
                User.FindFirstValue("CustomerId") ??
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(s) || !int.TryParse(s, out var id))
                throw new Exception("customerId introuvable dans le token.");
            return id;
        }
    }

    // ✅ LIST + SUMMARY
    [HttpGet]
    public async Task<IActionResult> List(int productId, string sort = "recent", int? stars = null, int page = 1, int pageSize = 10)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var baseQ = _db.ProductReviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId && !r.IsDeleted);

        if (stars is >= 1 and <= 5)
            baseQ = baseQ.Where(r => r.Rating == stars.Value);

        // summary (sur toutes les reviews non supprimées)
        var all = _db.ProductReviews.AsNoTracking().Where(r => r.ProductId == productId && !r.IsDeleted);

        var summary = new ReviewSummaryDto(
            Avg: await all.Select(x => (decimal?)x.Rating).AverageAsync() ?? 0,
            Count: await all.CountAsync(),
            Star1: await all.CountAsync(x => x.Rating == 1),
            Star2: await all.CountAsync(x => x.Rating == 2),
            Star3: await all.CountAsync(x => x.Rating == 3),
            Star4: await all.CountAsync(x => x.Rating == 4),
            Star5: await all.CountAsync(x => x.Rating == 5)
        );

        // tri
        baseQ = sort == "top"
            ? baseQ.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAtUtc)
            : baseQ.OrderByDescending(r => r.CreatedAtUtc);

        var total = await baseQ.CountAsync();

        var items = await baseQ
            .Include(r => r.Customer)
            .Select(r => new ReviewItemDto(
                r.Id,
                r.CustomerId,
                r.Customer.FullName,
                r.Rating,
                r.Title,
                r.Comment,
                (r.OrderId != null),
                r.CreatedAtUtc
            ))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { summary, total, page, pageSize, items });
    }

    // ✅ POST (Client) — ACHAT VÉRIFIÉ OBLIGATOIRE
    [Authorize(Roles = "Client")]
    [HttpPost]
    public async Task<IActionResult> Create(int productId, ReviewCreateDto dto)
    {
        if (dto.Rating < 1 || dto.Rating > 5) return BadRequest(new { message = "Rating invalide (1..5)." });

        // 1) vérifier achat (commande livrée + contient le produit)
        var deliveredOrderId = await _db.Orders
            .Where(o => o.CustomerId == CustomerId && o.Status == "Livree")
            .Join(_db.OrderItems.Where(oi => oi.ProductId == productId),
                  o => o.Id, oi => oi.OrderId,
                  (o, oi) => o.Id)
            .OrderByDescending(x => x)
            .FirstOrDefaultAsync();

        //if (deliveredOrderId == 0) return StatusCode(403, new { message = "Achat livré requis" });
        // 2) upsert (1 avis par produit/client)
        var existing = await _db.ProductReviews
            .FirstOrDefaultAsync(r => r.ProductId == productId
                                   && r.CustomerId == CustomerId
                                   && !r.IsDeleted);

        if (existing is null)
        {
            var review = new ProductReview
            {
                ProductId = productId,
                CustomerId = CustomerId,
                OrderId = deliveredOrderId,
                Rating = dto.Rating,
                Title = dto.Title?.Trim(),
                Comment = dto.Comment?.Trim(),
                IsDeleted = false,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.ProductReviews.Add(review);
        }
        else
        {
            existing.OrderId = deliveredOrderId; // garde trace achat vérifié
            existing.Rating = dto.Rating;
            existing.Title = dto.Title?.Trim();
            existing.Comment = dto.Comment?.Trim();
            existing.IsDeleted = false;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await RecalcProductRating(productId);

        var savedReview = await _db.ProductReviews
            .AsNoTracking()
            .Include(r => r.Customer)
            .Where(r => r.ProductId == productId
                     && r.CustomerId == CustomerId
                     && !r.IsDeleted)
            .Select(r => new
            {
                id = r.Id,
                rating = r.Rating,
                title = r.Title,
                comment = r.Comment,
                customerName = r.Customer != null ? r.Customer.FullName : "Client",
                verifiedPurchase = r.OrderId != null,
                createdAtUtc = r.CreatedAtUtc
            })
            .FirstOrDefaultAsync();

        return Ok(new { ok = true, item = savedReview });
    }

    // ✅ UPDATE (Client propriétaire)
    [Authorize(Roles = "Client")]
    [HttpPut("{reviewId:int}")]
    public async Task<IActionResult> Update(int productId, int reviewId, ReviewUpdateDto dto)
    {
        if (dto.Rating < 1 || dto.Rating > 5) return BadRequest(new { message = "Rating invalide (1..5)." });

        var r = await _db.ProductReviews.FirstOrDefaultAsync(x => x.Id == reviewId && x.ProductId == productId && !x.IsDeleted);
        if (r is null) return NotFound();
        if (r.CustomerId != CustomerId) return Forbid();

        r.Rating = dto.Rating;
        r.Title = dto.Title?.Trim();
        r.Comment = dto.Comment?.Trim();
        r.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await RecalcProductRating(productId);

        return Ok(new { ok = true });
    }

    // ✅ DELETE (soft)
    [Authorize(Roles = "Client")]
    [HttpDelete("{reviewId:int}")]
    public async Task<IActionResult> Delete(int productId, int reviewId)
    {
        var r = await _db.ProductReviews.FirstOrDefaultAsync(x => x.Id == reviewId && x.ProductId == productId && !x.IsDeleted);
        if (r is null) return NotFound();
        if (r.CustomerId != CustomerId) return Forbid();

        r.IsDeleted = true;
        r.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await RecalcProductRating(productId);

        return Ok(new { ok = true });
    }

    // ✅ recalcul stats dans Products
    private async Task RecalcProductRating(int productId)
    {
        var q = _db.ProductReviews.AsNoTracking().Where(x => x.ProductId == productId && !x.IsDeleted);
        var count = await q.CountAsync();
        var avg = count == 0 ? 0m : (await q.Select(x => (decimal)x.Rating).AverageAsync());

        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == productId);
        if (p is null) return;

        p.RatingCount = count;
        p.RatingAvg = Math.Round(avg, 2);

        await _db.SaveChangesAsync();
    }

    [HttpGet("/api/products/{productId:int}/can-review")]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> CanReview(int productId)
    {
        var customerId = CustomerId; // ta propriété (comme tu fais déjà)

        var q = await _db.Orders
            .Where(o => o.CustomerId == customerId && o.Status == "Livree")
            .SelectMany(o => o.Items.Select(i => new { o.Id, i.ProductId }))
            .FirstOrDefaultAsync(x => x.ProductId == productId);

        if (q == null)
            return Ok(new { canReview = false, reason = "Not delivered" });

        // option: empêcher double avis
        var exists = await _db.ProductReviews.AnyAsync(r =>
            r.ProductId == productId && r.CustomerId == customerId && !r.IsDeleted);

        if (exists)
            return Ok(new { canReview = false, reason = "Already reviewed" });

        return Ok(new { canReview = true, orderId = q.Id });
    }
}