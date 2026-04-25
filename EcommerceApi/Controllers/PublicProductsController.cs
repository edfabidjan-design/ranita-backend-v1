using EcommerceApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/products")]
public class PublicProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    public PublicProductsController(AppDbContext db) => _db = db;

    // GET /api/products/by-category/mode/homme/vetements-homme
    [HttpGet("by-category/{*fullSlug}")]
    public async Task<IActionResult> GetByCategoryPath([FromRoute] string fullSlug)
    {
        fullSlug = (fullSlug ?? "").Trim().Trim('/').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(fullSlug))
            return BadRequest(new { ok = false, message = "Chemin manquant." });

        var parts = fullSlug
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant())
            .ToArray();

        // ✅ 1) Charger catégories (ParentId normalisé)
        var cats = await _db.Categories
            .AsNoTracking()
            .Select(c => new
            {
                c.Id,
                ParentKey = c.ParentId ?? 0,   // root => 0
                Slug = (c.Slug ?? "").ToLower(),
                c.IsActive
            })
            .ToListAsync();

        // ✅ Map enfants: parentKey(int) => [childIds]
        var childrenMap = cats
            .GroupBy(x => x.ParentKey)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

        // ✅ 2) Trouver l'Id de la catégorie via le chemin
        int currentParentKey = 0; // root
        int? currentId = null;

        foreach (var slugPart in parts)
        {
            var found = cats.FirstOrDefault(x =>
                x.ParentKey == currentParentKey &&
                x.Slug == slugPart &&
                x.IsActive // ✅ catégorie active seulement
            );

            if (found == null)
                return NotFound(new { ok = false, message = "Catégorie introuvable." });

            currentId = found.Id;
            currentParentKey = found.Id;
        }

        if (currentId == null)
            return NotFound(new { ok = false, message = "Catégorie introuvable." });

        // ✅ 3) Récupérer tous les descendants (BFS)
        var ids = new HashSet<int> { currentId.Value };
        var q = new Queue<int>();
        q.Enqueue(currentId.Value);

        while (q.Count > 0)
        {
            var id = q.Dequeue();
            if (!childrenMap.TryGetValue(id, out var kids)) continue;

            foreach (var k in kids)
            {
                if (ids.Add(k))
                    q.Enqueue(k);
            }
        }

        // ✅ 4) Produits publics (marketplace rules)
        var products = await _db.Products
     .AsNoTracking()
     .Include(p => p.Images)
     .Where(p =>
         ids.Contains(p.CategoryId) &&
         !p.IsDeleted &&
         p.IsActive &&
         p.PublishedStatus == "Published"
     )
     .OrderByDescending(p => p.Id)
     .Select(p => new
     {
         id = p.Id,
         name = p.Name,
         slug = p.Slug,
         price = p.Price,
         pricePromo = p.PricePromo,
         stock = p.Variants.Any() ? p.Variants.Sum(v => v.Stock) : p.Stock,

         categoryId = p.CategoryId,
         mainImageUrl = p.Images
             .OrderByDescending(i => i.IsMain)   // ✅ main d’abord
             .ThenBy(i => i.SortOrder)
             .Select(i => i.Url)
             .FirstOrDefault()
     })
     .ToListAsync();


        return Ok(new { ok = true, categoryId = currentId.Value, items = products });
    }


}
