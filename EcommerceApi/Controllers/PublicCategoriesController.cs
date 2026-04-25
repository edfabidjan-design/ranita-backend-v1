using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/categories")]
public class PublicCategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    public PublicCategoriesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var rows = await _db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                Slug = c.Slug ?? "",
                ParentId = c.ParentId ?? 0,   // ✅ IMPORTANT
                c.ImageUrl,
          
                c.IsActive,
                c.SortOrder
            })

            .ToListAsync();

        return Ok(new { ok = true, items = rows });
    }



    // GET /api/categories/by-slug/{slug}
    [HttpGet("by-slug/{slug}")]
    public async Task<IActionResult> GetBySlug([FromRoute] string slug)
    {
        slug = (slug ?? "").Trim().ToLower();

        var c = await _db.Categories
            .AsNoTracking()
           .Where(x => x.Slug != null && x.Slug.ToLower() == slug)

            .Select(x => new
            {
                id = x.Id,
                name = x.Name,
                slug = x.Slug,
                imageUrl = x.ImageUrl,
                parentId = x.ParentId,
                sortOrder = x.SortOrder,
                isActive = x.IsActive,
             
                metaTitle = x.MetaTitle,
                metaDescription = x.MetaDescription,
                description = x.Description
            })
            .FirstOrDefaultAsync();

        if (c == null) return NotFound(new { ok = false, message = "Catégorie introuvable" });

        return Ok(new { ok = true, item = c });
    }

    // GET /api/categories/by-path/mode/homme/vetements-homme
    // GET /api/categories/by-path/mode/homme/vetements-homme
    [HttpGet("by-path/{*fullSlug}")]
    public async Task<IActionResult> GetByPath([FromRoute] string fullSlug)
    {
        fullSlug = (fullSlug ?? "").Trim().Trim('/').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(fullSlug))
            return BadRequest(new { ok = false, message = "Chemin manquant." });

        var parts = fullSlug
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant())
            .ToArray();

        // 1) Charger toutes les catégories nécessaires
        var rows = await _db.Categories
            .AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Slug,
                c.ImageUrl,
                c.ParentId,
                c.SortOrder,
                c.IsActive,
            
                c.MetaTitle,
                c.MetaDescription,
                c.Description
            })
            .ToListAsync();

        if (rows.Count == 0)
            return NotFound(new { ok = false, message = "Aucune catégorie." });

        // 2) Index par Id (pour remonter le breadcrumb)
        var byId = rows.ToDictionary(x => x.Id);

        // 3) Index (ParentId + slugLower) => node (pour résoudre le chemin vite)
        //    ParentId = null pour racine.
        var byParentSlug = rows
     .Where(x => !string.IsNullOrWhiteSpace(x.Slug))
     .GroupBy(x => new
     {
         ParentId = x.ParentId,
         Slug = x.Slug!.Trim().ToLowerInvariant()
     })
     .ToDictionary(
         g => (g.Key.ParentId, g.Key.Slug),
         g => g.First()
     );
        ;

        // 4) children map (pour hasChildren)
        var childrenMap = rows
            .GroupBy(x => x.ParentId ?? 0) // 0 = racine (juste pour le map)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 5) Résoudre le chemin /mode/homme/...
        int? currentParent = null;
        int? currentId = null;

        foreach (var slugPart in parts)
        {
            if (!byParentSlug.TryGetValue((currentParent, slugPart), out var found))
                return NotFound(new { ok = false, message = "Catégorie introuvable." });

            currentId = found.Id;
            currentParent = found.Id;
        }

        if (currentId == null)
            return NotFound(new { ok = false, message = "Catégorie introuvable." });

        // 6) Breadcrumb : remonte les parents jusqu'à racine
        List<int> BuildPathIds(int id)
        {
            var path = new List<int>();
            var cur = id;
            var guard = 0;

            while (true)
            {
                if (!byId.TryGetValue(cur, out var node)) break;

                path.Add(cur);

                if (node.ParentId == null) break;

                cur = node.ParentId.Value;
                guard++;
                if (guard > 50) break;
            }

            path.Reverse();
            return path;
        }

        var node = byId[currentId.Value];

        var pathIds = BuildPathIds(node.Id);
        var pathNames = pathIds.Select(pid => byId[pid].Name).ToList();

        // ✅ pathSlugs safe (pas de null)
        var pathSlugs = pathIds
            .Select(pid => (byId[pid].Slug ?? "").Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var full = string.Join("/", pathSlugs);

        var hasChildren = childrenMap.ContainsKey(node.Id);

        return Ok(new
        {
            ok = true,
            item = new
            {
                id = node.Id,
                name = node.Name,
                slug = node.Slug,
                fullSlug = full,
                pathNames,
                pathSlugs,
                imageUrl = node.ImageUrl,
                parentId = node.ParentId,
                sortOrder = node.SortOrder,
                isActive = node.IsActive,
            
                metaTitle = node.MetaTitle,
                metaDescription = node.MetaDescription,
                description = node.Description,
                hasChildren
            }
        });
    }

    [HttpGet("{id:int}/config")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryConfig(int id)
    {
        var category = await _db.Categories
            .Include(c => c.CategoryAttributes)
                .ThenInclude(ca => ca.Attribute)
                    .ThenInclude(a => a.Options)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null) return NotFound();

        var variantAttributes = category.CategoryAttributes
     .Where(ca => ca.IsVariant)
     .Select(ca => new {
         id = ca.Attribute.Id,
         name = ca.Attribute.Name,
         options = ca.Attribute.Options
             .OrderBy(o => o.SortOrder)
             .Select(o => new { id = o.Id, value = o.Value })
     });

        var productAttributes = category.CategoryAttributes
     .Where(ca => !ca.IsVariant)
     .Select(ca => new {
         id = ca.Attribute.Id,
         name = ca.Attribute.Name,
         type = ca.Attribute.DataType, // "Text" / "Option" / etc...
         options = ca.Attribute.Options
             .OrderBy(o => o.SortOrder)
             .Select(o => new { id = o.Id, value = o.Value })
             .ToList()
     });
        return Ok(new { variantAttributes, productAttributes });
    }

    [HttpGet("{catId:int}/attributes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryAttributes(int catId)
    {
        var rows = await _db.CategoryAttributes
            .AsNoTracking()
            .Where(x => x.CategoryId == catId)
            .OrderBy(x => x.SortOrder)
            .Include(x => x.Attribute)
                .ThenInclude(a => a.Options)
            .ToListAsync();

        var outRows = rows.Select(x => new
        {
            attributeId = x.AttributeId,
            isRequired = x.IsRequired,
            isFilterable = x.IsFilterable,
            sortOrder = x.SortOrder,
            // IMPORTANT : ton JS vendor lit attribute.isVariant, donc on l’envoie
            attribute = new
            {
                id = x.Attribute.Id,
                code = x.Attribute.Code,
                name = x.Attribute.Name,
                dataType = x.Attribute.DataType.ToString(), // "Text" / "Option"
                isVariant = x.Attribute.IsVariant,
                options = (x.Attribute.Options ?? new List<ProductAttributeOption>())
                    .OrderBy(o => o.SortOrder)
                    .Select(o => new { id = o.Id, value = o.Value, sortOrder = o.SortOrder })
                    .ToList()
            }
        }).ToList();

        return Ok(outRows);
    }

    [AllowAnonymous]
    [HttpGet("popular")]
    public async Task<IActionResult> GetPopularCategories([FromQuery] int take = 8)
    {
        take = take <= 0 ? 8 : Math.Min(take, 12);

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Slug,
                c.ImageUrl,
                c.ParentId,
                c.SortOrder,
                c.IsActive
            })
            .ToListAsync();

        var products = await _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted)
            .Select(p => new
            {
                p.Id,
                p.CategoryId
            })
            .ToListAsync();

        var orderItems = await _db.OrderItems
            .AsNoTracking()
            .Select(oi => new
            {
                oi.ProductId,
                oi.Quantity
            })
            .ToListAsync();

        var categoryMap = categories.ToDictionary(c => c.Id, c => c);

        int GetRootCategoryId(int categoryId)
        {
            var visited = new HashSet<int>();
            var currentId = categoryId;

            while (categoryMap.TryGetValue(currentId, out var current) && current.ParentId.HasValue)
            {
                if (!visited.Add(currentId))
                    break;

                currentId = current.ParentId.Value;
            }

            return currentId;
        }

        var productSalesMap = orderItems
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => x.Quantity)
            );

        var grouped = products
            .Where(p => categoryMap.ContainsKey(p.CategoryId))
            .GroupBy(p => GetRootCategoryId(p.CategoryId))
            .Select(g =>
            {
                var root = categoryMap[g.Key];
                var productCount = g.Count();
                var salesCount = g.Sum(p => productSalesMap.TryGetValue(p.Id, out var qty) ? qty : 0);
                var score = (salesCount * 5) + productCount;

                return new
                {
                    root.Id,
                    root.Name,
                    root.Slug,
                    root.ImageUrl,
                    root.ParentId,
                    root.SortOrder,
                    ProductCount = productCount,
                    SalesCount = salesCount,
                    Score = score
                };
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.SalesCount)
            .ThenByDescending(x => x.ProductCount)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Take(take)
            .ToList();

        return Ok(grouped);
    }
}