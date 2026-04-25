using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProductsController(AppDbContext db) => _db = db;

  
    // ✅ PUBLIC LIST (avec filtres)
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll(
    [FromQuery] int? categoryId = null,
    [FromQuery] int? vendorId = null,
    [FromQuery] int take = 0)
    {
        var q = _db.Products.AsNoTracking()
            .Where(p =>
                !p.IsDeleted
                && p.IsActive
                && p.PublishedStatus == "Published"
                && (p.VendorId == null || (p.Vendor != null && p.Vendor.Status == 1))
            )
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Vendor)   // 🔥 IMPORTANT
            .AsQueryable();

        if (categoryId.HasValue)
        {
            var categoryIds = await GetDescendantCategoryIds(categoryId.Value);
            q = q.Where(p => categoryIds.Contains(p.CategoryId));
        }

        if (vendorId.HasValue)
            q = q.Where(p => p.VendorId == vendorId.Value);

        q = q.OrderBy(p => p.Name);

        if (take > 0)
            q = q.Take(take);


        // ✅ Résumé avis par produit (1 requête groupée)
        var reviewSummary = await _db.ProductReviews
          .AsNoTracking()
          .GroupBy(r => r.ProductId)
          .Select(g => new
          {
              ProductId = g.Key,
              Count = g.Count(),
              Avg = g.Average(x => (double)x.Rating),
              VerifiedCount = g.Count(x => x.OrderId != null) // ✅ simple
          })
          .ToDictionaryAsync(x => x.ProductId);

        var items = await q.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            slug = p.Slug,

            price = p.Price,
            pricePromo = p.PricePromo,

            reviewsCount = reviewSummary.ContainsKey(p.Id) ? reviewSummary[p.Id].Count : 0,
            ratingAvg = reviewSummary.ContainsKey(p.Id) ? reviewSummary[p.Id].Avg : 0,
            verifiedReviewsCount = reviewSummary.ContainsKey(p.Id) ? reviewSummary[p.Id].VerifiedCount : 0,

            stock = p.Variants.Any() ? p.Variants.Sum(v => v.Stock) : p.Stock,

            brand = p.Brand,
            sku = p.Sku,

            variantsCount = p.Variants.Count,

            shortDescription = p.ShortDescription,
            description = p.Description,
            longDescription = p.LongDescription,

            highlights = p.Highlights,
            weightKg = p.WeightKg,
            dimensions = p.Dimensions,

            // ✅ AJOUT IMPORTANT
            categoryId = p.CategoryId,

            categoryName = p.Category != null ? p.Category.Name : null,
            categorySlug = p.Category != null ? p.Category.Slug : null,

            vendorId = p.VendorId,
            vendorShopName = p.Vendor != null ? p.Vendor.ShopName : null,
            vendorName = p.Vendor != null ? p.Vendor.Name : null,

            mainImageUrl = p.Images

    .OrderByDescending(i => i.IsMain)
    .ThenBy(i => i.SortOrder)
    .Select(i => i.Url)
    .FirstOrDefault()
        })
        .ToListAsync();

        return Ok(new { ok = true, items });
    }

    // ✅ PUBLIC DETAIL
    // ✅ PUBLIC DETAIL
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOne(int id)
    {
        var p = await _db.Products.AsNoTracking()
            .Where(x =>
                x.Id == id
                && !x.IsDeleted
                && x.IsActive
                && x.PublishedStatus == "Published"
                && (x.VendorId == null || (x.Vendor != null && x.Vendor.Status == 1))
            )
            .Include(x => x.Vendor)
            .Include(x => x.Category)
            .Include(x => x.Images)
            .Include(x => x.Variants)
            .FirstOrDefaultAsync();

        if (p == null)
            return NotFound(new { ok = false, message = "Produit introuvable." });

        var images = p.Images.OrderBy(i => i.SortOrder).ToList();
        var main = images.FirstOrDefault(i => i.IsMain)?.Url;

        // ✅ ATTRIBUTS (groupés)
        var attrRows = await _db.ProductAttributeValues
            .AsNoTracking()
            .Where(v => v.ProductId == p.Id)
            .Include(v => v.Attribute)
            .Include(v => v.Option)
            .ToListAsync();

        var attributes = attrRows
            .GroupBy(v => new { v.Attribute.Code, v.Attribute.Name })
            .Select(g => new
            {
                code = g.Key.Code,
                name = g.Key.Name,
                values = g.Select(v =>
                        !string.IsNullOrWhiteSpace(v.ValueText) ? v.ValueText :
                        (v.Option != null ? v.Option.Value : "")
                    )
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct()
                    .ToList()
            })
            .ToList();

        var rawSizes = p.Sizes ?? "";
        var rawColors = p.Colors ?? "";

        List<string> splitVals(string raw) =>
            (raw ?? "")
                .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        bool LooksLikeColor(string s)
        {
            s = (s ?? "").Trim().ToLowerInvariant();
            return new[]
            {
        "noir","blanc","bleu","rouge","vert","jaune",
        "marron","gris","rose","violet","argent","doré","inox"
    }.Contains(s);
        }

        bool LooksLikeClothSize(string s)
        {
            s = (s ?? "").Trim().ToLowerInvariant();
            return new[] { "xs", "s", "m", "l", "xl", "xxl", "xxxl" }.Contains(s);
        }

        bool LooksLikeNumericSize(string s)
        {
            s = (s ?? "").Trim().ToLowerInvariant();
            return System.Text.RegularExpressions.Regex.IsMatch(s, @"^\d+$");
        }

        bool LooksLikeScreenSize(string s)
        {
            s = (s ?? "").Trim().ToLowerInvariant().Replace(" ", "");
            return System.Text.RegularExpressions.Regex.IsMatch(s, @"^\d{2,3}$")
                || System.Text.RegularExpressions.Regex.IsMatch(s, @"^\d{2,3}p$")
                || System.Text.RegularExpressions.Regex.IsMatch(s, @"^\d{2,3}(""\u201d)?$");
        }

        var sizesListFixed = splitVals(rawSizes);
        var colorsListFixed = splitVals(rawColors);

        // corrige inversion tailles/couleurs
        if (sizesListFixed.Count > 0 && colorsListFixed.Count > 0
            && sizesListFixed.All(LooksLikeColor)
            && colorsListFixed.All(x => LooksLikeNumericSize(x) || LooksLikeClothSize(x) || LooksLikeScreenSize(x)))
        {
            var tmp = sizesListFixed;
            sizesListFixed = colorsListFixed;
            colorsListFixed = tmp;
        }

        // fallback depuis Variants si Sizes/Colors vides
        if (!sizesListFixed.Any() && !colorsListFixed.Any())
        {
            sizesListFixed = p.Variants
                .Select(v => (v.Size ?? "").Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x) && !x.Equals("Unique", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            colorsListFixed = p.Variants
                .Select(v => (v.Color ?? "").Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x) && !x.Equals("Unique", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var axis1LabelComputed = "Options disponibles";
        var axis2LabelComputed = "";

        if (sizesListFixed.Any() && !colorsListFixed.Any())
        {
            if (sizesListFixed.All(LooksLikeScreenSize))
                axis1LabelComputed = "Tailles écran disponibles";
            else if (sizesListFixed.All(LooksLikeClothSize))
                axis1LabelComputed = "Tailles disponibles";
            else if (sizesListFixed.All(LooksLikeNumericSize))
                axis1LabelComputed = "Pointures disponibles";
            else
                axis1LabelComputed = "Options disponibles";
        }
        else if (sizesListFixed.Any() && colorsListFixed.Any())
        {
            if (sizesListFixed.All(LooksLikeScreenSize))
                axis1LabelComputed = "Tailles écran disponibles";
            else if (sizesListFixed.All(LooksLikeClothSize))
                axis1LabelComputed = "Tailles disponibles";
            else if (sizesListFixed.All(LooksLikeNumericSize))
                axis1LabelComputed = "Pointures disponibles";
            else
                axis1LabelComputed = "Options disponibles";

            axis2LabelComputed = "Couleurs disponibles";
        }
        else if (!sizesListFixed.Any() && colorsListFixed.Any())
        {
            axis1LabelComputed = "Couleurs disponibles";
            axis2LabelComputed = "";
        }

        return Ok(new
        {
            ok = true,
            item = new
            {
                id = p.Id,
                name = p.Name,

                vendorId = p.VendorId,
                vendorName = p.Vendor != null ? p.Vendor.Name : null,
                vendorShopName = p.Vendor != null ? p.Vendor.ShopName : null,

                price = p.Price,
                pricePromo = p.PricePromo,

                shortDescription = p.ShortDescription,
                description = p.Description,
                longDescription = p.LongDescription,
                highlights = p.Highlights,

                brand = p.Brand,
                sku = p.Sku,
                weightKg = p.WeightKg,
                dimensions = p.Dimensions,

                stock = p.Variants.Any()
                    ? p.Variants.Sum(v => v.Stock)
                    : p.Stock,

                variantMode =
                    p.Variants.Any(v => !string.IsNullOrWhiteSpace(v.Size) && !string.IsNullOrWhiteSpace(v.Color))
                        ? "double_axis"
                        : p.Variants.Any(v => !string.IsNullOrWhiteSpace(v.Size) || !string.IsNullOrWhiteSpace(v.Color))
                            ? "single_axis"
                            : "none",

                axis1Label = axis1LabelComputed,
                axis2Label = axis2LabelComputed,

                sizes = string.Join(";", sizesListFixed),
                colors = string.Join(";", colorsListFixed),

                variants = p.Variants.Select(v => new
                {
                    id = v.Id,
                    size = v.Size ?? "",
                    color = v.Color ?? "",
                    stock = v.Stock,
                    label =
                        !string.IsNullOrWhiteSpace(v.Size) && !string.IsNullOrWhiteSpace(v.Color)
                            ? $"{v.Size} / {v.Color}"
                            : !string.IsNullOrWhiteSpace(v.Size)
                                ? v.Size
                                : !string.IsNullOrWhiteSpace(v.Color)
                                    ? v.Color
                                    : "Unique"
                }).ToList(),

                category = p.Category == null ? null : new
                {
                    id = p.Category.Id,
                    name = p.Category.Name,
                    slug = p.Category.Slug
                },

                mainImageUrl = main,
                images = images.Select(i => new
                {
                    id = i.Id,
                    url = i.Url,
                    isMain = i.IsMain,
                    sortOrder = i.SortOrder
                }),

                attributes = attributes
            }
        });
    }

    // ✅ PUBLIC : Fréquemment achetés ensemble (basé sur les commandes)
    // GET /api/products/{id}/bought-together?take=8
    [AllowAnonymous]
    [HttpGet("{id:int}/bought-together")]
    public async Task<IActionResult> BoughtTogether(int id, [FromQuery] int take = 8)
    {
        take = Math.Clamp(take, 1, 20);

        // 1) Trouver les commandes qui contiennent ce produit
        var orderIds = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.ProductId == id)
            .Select(oi => oi.OrderId)
            .Distinct()
            .ToListAsync();

        if (orderIds.Count == 0)
            return Ok(new { ok = true, items = Array.Empty<object>() });

        // 2) Trouver les autres produits achetés dans ces mêmes commandes (score)
        var scored = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => orderIds.Contains(oi.OrderId) && oi.ProductId != id)
            .GroupBy(oi => oi.ProductId)
            .Select(g => new { ProductId = g.Key, Score = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.Score)
            .Take(take)
            .ToListAsync();

        if (scored.Count == 0)
            return Ok(new { ok = true, items = Array.Empty<object>() });

        var ids = scored.Select(x => x.ProductId).ToList();

        // 3) Récupérer les produits (même shape que tes listes)
        var products = await _db.Products
            .AsNoTracking()
            .Where(p =>
    ids.Contains(p.Id)
    && !p.IsDeleted
    && p.IsActive
    && p.PublishedStatus == "Published"
    && (p.VendorId == null || (p.Vendor != null && p.Vendor.Status == 1))
)
.Include(p => p.Vendor)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Select(p => new
            {
                id = p.Id,
                name = p.Name,
                slug = p.Slug,

                price = p.Price,
                pricePromo = p.PricePromo,

                stock = p.Variants.Any() ? p.Variants.Sum(v => v.Stock) : p.Stock,

                brand = p.Brand,
                sku = p.Sku,

                shortDescription = p.ShortDescription,
                description = p.Description,
                longDescription = p.LongDescription,

                highlights = p.Highlights,
                weightKg = p.WeightKg,
                dimensions = p.Dimensions,

                categoryId = p.CategoryId,

                mainImageUrl = p.Images
                    .Where(i => i.IsMain)
                    .OrderBy(i => i.SortOrder)
                    .Select(i => i.Url)
                    .FirstOrDefault()
            })
            .ToListAsync();

        // 4) Re-garder l’ordre par score
        var map = products.ToDictionary(x => x.id, x => x);
        var ordered = scored
            .Where(s => map.ContainsKey(s.ProductId))
            .Select(s => map[s.ProductId])
            .ToList();

        return Ok(new { ok = true, items = ordered });
    }

    [AllowAnonymous]
    [HttpGet("popular")]
    public async Task<IActionResult> Popular([FromQuery] int minClients = 2, [FromQuery] int take = 8)
    {
        take = Math.Clamp(take, 1, 20);
        minClients = Math.Clamp(minClients, 1, 100);

        var scored = await _db.OrderItems.AsNoTracking()
            .Join(_db.Orders.AsNoTracking(),
                  oi => oi.OrderId,
                  o => o.Id,
                  (oi, o) => new
                  {
                      oi.ProductId,
                      o.Status,
                      ClientKey = o.CustomerId != null
                          ? ("C:" + o.CustomerId)
                          : ("E:" + ((o.Email ?? "").Trim().ToLower()))
                  })
            .Where(x => x.Status == "Livree")          // ✅ IMPORTANT: seulement livrées
            .Where(x => x.ClientKey != "E:")          // ignore email vide
            .GroupBy(x => x.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                UniqueClients = g.Select(x => x.ClientKey).Distinct().Count()
            })
            .Where(x => x.UniqueClients >= minClients)
            .OrderByDescending(x => x.UniqueClients)
            .Take(take)
            .ToListAsync();

        if (!scored.Any())
            return Ok(new { ok = true, items = Array.Empty<object>() });

        var ids = scored.Select(x => x.ProductId).ToList();

        var products = await _db.Products.AsNoTracking()
            .Where(p =>
    ids.Contains(p.Id)
    && !p.IsDeleted
    && p.IsActive
    && p.PublishedStatus == "Published"
    && (p.VendorId == null || (p.Vendor != null && p.Vendor.Status == 1))
)
.Include(p => p.Vendor)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Select(p => new
            {
                id = p.Id,
                name = p.Name,
                slug = p.Slug,
                price = p.Price,
                pricePromo = p.PricePromo,
                stock = p.Variants.Any() ? p.Variants.Sum(v => v.Stock) : p.Stock,
                mainImageUrl = p.Images
                    .Where(i => i.IsMain)
                    .OrderBy(i => i.SortOrder)
                    .Select(i => i.Url)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var map = products.ToDictionary(x => x.id, x => x);
        var ordered = scored.Where(s => map.ContainsKey(s.ProductId)).Select(s => map[s.ProductId]).ToList();

        return Ok(new { ok = true, items = ordered });
    }

    private async Task<List<int>> GetDescendantCategoryIds(int rootId)
    {
        var all = await _db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new
            {
                c.Id,
                c.ParentId
            })
            .ToListAsync();

        var result = new List<int> { rootId };
        var queue = new Queue<int>();
        queue.Enqueue(rootId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            var children = all
                .Where(x => x.ParentId == current)
                .Select(x => x.Id)
                .ToList();

            foreach (var childId in children)
            {
                if (!result.Contains(childId))
                {
                    result.Add(childId);
                    queue.Enqueue(childId);
                }
            }
        }

        return result;
    }
}
