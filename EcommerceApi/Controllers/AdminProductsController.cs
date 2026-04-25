using EcommerceApi.Data;
using EcommerceApi.Dtos;
using EcommerceApi.Helpers;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[ApiController]
[Route("api/admin/products")]
[Authorize]
public class AdminProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public AdminProductsController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // ✅ GET ALL (avec soft delete filter)

    [HttpGet]
    public async Task<IActionResult> GetAll(
    [FromQuery] string? q,
    [FromQuery] int? categoryId,
    [FromQuery] bool showInactive = false
)
    {
        if (!await HasPermission("products.view"))
            return Forbid();

        var query = _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .AsQueryable();

        query = query.Where(p => !p.IsDeleted);

        if (!showInactive)
            query = query.Where(p => p.IsActive);



        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Name.Contains(q));

        if (categoryId.HasValue && categoryId.Value > 0)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        var items = await query
            .OrderByDescending(p => p.Id)
            .Select(p => new ProductListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Price = p.Price,
                Stock = p.Variants.Any() ? p.Variants.Sum(v => v.Stock) : p.Stock,
                IsActive = p.IsActive,
                MainImageUrl = p.Images
                    .OrderByDescending(i => i.IsMain)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => i.Url)
                    .FirstOrDefault(),
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : null,
                PricePromo = p.PricePromo,
                WeightKg = p.WeightKg,
                Dimensions = p.Dimensions
            })
            .ToListAsync();

        return Ok(new { ok = true, items });
    }


    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        if (!await HasPermission("products.manage"))
            return Forbid();

        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { ok = false, message = "Nom obligatoire." });

        if (dto.Price <= 0)
            return BadRequest(new { ok = false, message = "Prix invalide." });

        var slugBase = SlugHelper.Slugify(dto.Name);
        var slug = await EnsureUniqueSlugAsync(slugBase);

        var sku = (dto.Sku ?? "").Trim();
        if (string.IsNullOrWhiteSpace(sku))
            return BadRequest(new { ok = false, message = "SKU obligatoire." });

        sku = sku.ToUpperInvariant();

        var existsSku = await _db.Products.AnyAsync(x => x.Sku == sku);
        if (existsSku)
            return BadRequest(new { ok = false, message = "SKU déjà utilisé." });

        // Optionnel : vérifier que le vendeur existe si un vendorId est fourni
        if (dto.VendorId.HasValue && dto.VendorId.Value > 0)
        {
            var vendorExists = await _db.Vendors.AnyAsync(v => v.Id == dto.VendorId.Value);
            if (!vendorExists)
                return BadRequest(new { ok = false, message = "Boutique vendeur introuvable." });
        }

        var p = new Product
        {
            Name = dto.Name.Trim(),
            Slug = slug,
            ShortDescription = dto.ShortDescription,
            Description = dto.Description,
            Price = dto.Price,
            Stock = Math.Max(0, dto.Stock),
            CategoryId = dto.CategoryId,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            VendorId = dto.VendorId,
            PricePromo = dto.PricePromo,
            Brand = dto.Brand,
            Sku = sku,
            LongDescription = dto.LongDescription,
            Highlights = dto.Highlights,
            WeightKg = dto.WeightKg,
            Dimensions = dto.Dimensions
        };

        if (p.IsActive)
            p.PublishedStatus = "Published";

        _db.Products.Add(p);
        await _db.SaveChangesAsync();

        return Ok(new { ok = true, id = p.Id, slug = p.Slug });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
    {
        if (!await HasPermission("products.manage"))
            return Forbid();

        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound(new { ok = false, message = "Produit introuvable." });

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { ok = false, message = "Nom obligatoire." });

        if (dto.Price <= 0)
            return BadRequest(new { ok = false, message = "Prix invalide." });

        var newName = dto.Name.Trim();
        if (!string.Equals(newName, p.Name, StringComparison.OrdinalIgnoreCase))
        {
            var slugBase = SlugHelper.Slugify(newName);
            p.Slug = await EnsureUniqueSlugAsync(slugBase, p.Id);
            p.Name = newName;
        }

        p.ShortDescription = dto.ShortDescription;
        p.Description = dto.Description;
        p.Price = dto.Price;
        p.Stock = Math.Max(0, dto.Stock);
        p.CategoryId = dto.CategoryId;
        p.IsActive = dto.IsActive;
        p.UpdatedAt = DateTime.UtcNow;

        p.PricePromo = dto.PricePromo;
        p.Brand = dto.Brand;

        // ✅ SKU UNIQUE (Update)
        // ✅ SKU : en UPDATE => facultatif
        // - si dto.Sku vide => on NE touche PAS au SKU existant
        // - si dto.Sku rempli => on vérifie unicité (en excluant le produit courant) puis on update
        var skuRaw = (dto.Sku ?? "").Trim();

        if (!string.IsNullOrWhiteSpace(skuRaw))
        {
            var sku = skuRaw.ToUpperInvariant();

            var existsSku = await _db.Products.AnyAsync(x => x.Id != id && x.Sku == sku);
            if (existsSku)
                return BadRequest(new { ok = false, message = "SKU déjà utilisé." });

            p.Sku = sku;
        }
        // else: on laisse p.Sku tel quel




        p.LongDescription = dto.LongDescription;
        p.Highlights = dto.Highlights;
        p.WeightKg = dto.WeightKg;
        p.Dimensions = dto.Dimensions;
        if (p.IsActive)
            p.PublishedStatus = "Published";
        else
            p.PublishedStatus = "Rejected"; // ou "Pending" selon ton workflow


        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    // ✅ Soft delete (désactiver)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await HasPermission("products.manage"))
            return Forbid();

        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound(new { ok = false, message = "Produit introuvable." });

        p.IsActive = false;
        p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { ok = true });
    }

    // Upload images : POST /api/admin/products/{id}/images (multipart)
    [HttpPost("{id:int}/images")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> UploadImages(int id, [FromForm] List<IFormFile> files, [FromForm] bool setFirstAsMain = true)
    {
        if (!await HasPermission("products.manage"))
            return Forbid();

        var p = await _db.Products.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound(new { ok = false, message = "Produit introuvable." });
        if (files == null || files.Count == 0) return BadRequest(new { ok = false, message = "Aucun fichier." });

        var dir = Path.Combine(_env.ContentRootPath, "uploads", "products", id.ToString());
        Directory.CreateDirectory(dir);

        bool hasMain = p.Images.Any(i => i.IsMain);
        int sortBase = p.Images.Any() ? p.Images.Max(i => i.SortOrder) + 1 : 0;

        foreach (var f in files)
        {
            if (f.Length <= 0) continue;

            var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(ext))
                return BadRequest(new { ok = false, message = "Format image non supporté (jpg/png/webp)." });

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(dir, fileName);

            using (var stream = System.IO.File.Create(fullPath))
                await f.CopyToAsync(stream);

            var url = $"/uploads/products/{id}/{fileName}";

            var img = new ProductImage
            {
                ProductId = id,
                Url = url,
                SortOrder = sortBase++
            };

            if (!hasMain && setFirstAsMain)
            {
                img.IsMain = true;
                hasMain = true;
            }

            _db.ProductImages.Add(img);
        }

        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpDelete("{id:int}/images/{imageId:int}")]
    public async Task<IActionResult> DeleteImage(int id, int imageId)
    {
        if (!await HasPermission("products.manage"))
            return Forbid();

        var img = await _db.ProductImages.FirstOrDefaultAsync(x => x.Id == imageId && x.ProductId == id);
        if (img == null) return NotFound(new { ok = false, message = "Image introuvable." });

        _db.ProductImages.Remove(img);
        await _db.SaveChangesAsync();

        try
        {
            if (!string.IsNullOrWhiteSpace(img.Url) &&
                img.Url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                var rel = img.Url.TrimStart('/')
                    .Replace("/", Path.DirectorySeparatorChar.ToString());

                var path = Path.Combine(_env.ContentRootPath, rel);

                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
        }
        catch { }

        return Ok(new { ok = true });
    }

    [HttpPost("{id:int}/images/{imageId:int}/main")]
    public async Task<IActionResult> SetMain(int id, int imageId)
    {
        if (!await HasPermission("products.manage"))
            return Forbid();

        var imgs = await _db.ProductImages.Where(x => x.ProductId == id).ToListAsync();
        if (imgs.Count == 0) return NotFound(new { ok = false, message = "Aucune image." });

        foreach (var i in imgs) i.IsMain = (i.Id == imageId);
        await _db.SaveChangesAsync();

        return Ok(new { ok = true });
    }

    // ✅ GET ONE (PRO SAFE)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOne(int id)
    {
        if (!await HasPermission("products.view"))
            return Forbid();

        try
        {
            // 1) Produit (sans Include)
            var p = await _db.Products.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Slug,
                    x.Price,
                    x.Stock,
                    x.IsActive,

                    x.PublishedStatus, // ✅ AJOUT
                    x.IsDeleted,       // ✅ AJOUT
                    x.CreatedAt,       // ✅ AJOUT
                    x.VendorId,        // ✅ AJOUT

                    x.RejectReason,
                    x.RejectedAt,

                    x.CategoryId,
                    x.ShortDescription,
                    x.Description,
                    x.PricePromo,
                    x.Brand,
                    x.Sku,
                    x.LongDescription,
                    x.Highlights,
                    x.WeightKg,
                    x.Dimensions,
                    CategoryName = x.Category != null ? x.Category.Name : null
                })
                .FirstOrDefaultAsync();

            if (p == null)
                return NotFound(new { ok = false, message = "Produit introuvable." });

            // 2) Variants (séparé)
            var variants = await _db.ProductVariants.AsNoTracking()
                .Where(v => v.ProductId == id)
                .OrderBy(v => v.Key1 ?? v.Size)   // ✅ robuste (nouveau + ancien)
                .ThenBy(v => v.Key2 ?? v.Color)
                .Select(v => new
                {
                    v.Id,
                    // ✅ renvoyer key1/key2 (TON JS utilise key1/key2)
                    Key1 = v.Key1 ?? v.Size,
                    Key2 = v.Key2 ?? v.Color,
                    v.Stock,

                    // optionnel (compat)
                    v.Size,
                    v.Color
                })
                .ToListAsync();

            // 3) Images (séparé)
            var images = await _db.ProductImages.AsNoTracking()
                .Where(i => i.ProductId == id)
                .OrderBy(i => i.SortOrder)
                .Select(i => new { i.Id, i.Url, i.IsMain, i.SortOrder })
                .ToListAsync();

            // 4) Attributs (lisibles)
            var attrRows = await _db.ProductAttributeValues.AsNoTracking()
                .Where(v => v.ProductId == id)
                .Include(v => v.Attribute)
                .Include(v => v.Option)
                .ToListAsync();


            var attributes = attrRows
                .Select(v => new
                {
                    attributeId = v.AttributeId,
                    optionId = v.OptionId,
                    valueText = !string.IsNullOrWhiteSpace(v.ValueText)
                        ? v.ValueText
                        : (v.Option != null ? v.Option.Value : null)
                })
                .ToList();


            return Ok(new
            {
                ok = true,
                item = new
                {
                    p.Id,
                    p.Name,
                    p.Slug,
                    p.Price,
                    p.Stock,
                    p.IsActive,

                    p.RejectReason,
                    p.RejectedAt,

                    PublishedStatus = p.PublishedStatus, // ✅ AJOUT
                    IsDeleted = p.IsDeleted,             // ✅ AJOUT
                    CreatedAt = p.CreatedAt,             // ✅ AJOUT
                    VendorId = p.VendorId,               // ✅ AJOUT

                    p.CategoryId,
                    p.CategoryName,
                    p.ShortDescription,
                    p.Description,
                    p.PricePromo,
                    p.Brand,
                    p.Sku,
                    p.LongDescription,
                    p.Highlights,
                    p.WeightKg,
                    p.Dimensions,

                    Variants = variants,
                    Images = images,
                    attributes = attributes
                }
            });
        }
        catch (Exception ex)
        {
            // ✅ en dev tu peux exposer ex.Message (sinon log seulement en prod)
            return StatusCode(500, new { ok = false, message = "Erreur serveur GetOne.", detail = ex.Message });
        }
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug, int? currentProductId = null)
    {
        var slug = string.IsNullOrWhiteSpace(baseSlug) ? "product" : baseSlug;
        int n = 1;

        while (true)
        {
            var exists = await _db.Products.AnyAsync(p =>
                p.Slug == slug && (!currentProductId.HasValue || p.Id != currentProductId.Value));

            if (!exists) return slug;

            n++;
            slug = $"{baseSlug}-{n}";
        }
    }

    // ====== VARIANTS + ATTRIBUTES endpoints ======
    public class ReplaceVariantsRequest
    {
        public List<VariantRow> Items { get; set; } = new();
    }

    public class VariantRow
    {
        // ✅ compat ancien / nouveau
        public string? Key1 { get; set; }
        public string? Key2 { get; set; }

        public string? Size { get; set; }
        public string? Color { get; set; }

        public int Stock { get; set; }
    }

    [HttpPost("{id:int}/variants")]
    public async Task<IActionResult> ReplaceVariants(int id, [FromBody] ReplaceVariantsRequest req)
    {
        if (!await HasPermission("products.manage"))
            return Forbid();

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
            return NotFound(new { ok = false, message = "Produit introuvable." });

        static string? CleanNull(string? v)
        {
            var s = (v ?? "").Trim();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        static string NormUnique(string? v)
            => string.IsNullOrWhiteSpace(v) ? "Unique" : v.Trim();

        var raw = req?.Items ?? new List<VariantRow>();

        // ✅ lire d'abord Key1/Key2, fallback Size/Color
        var normalized = raw
            .Select(x =>
            {
                var k1 = CleanNull(x.Key1) ?? CleanNull(x.Size);
                var k2 = CleanNull(x.Key2) ?? CleanNull(x.Color);

                // si tu veux "Unique" en UI, tu peux garder ça,
                // mais en DB on peut stocker null pour Key2 si 1D
                return new
                {
                    Key1 = k1,
                    Key2 = k2,
                    Stock = Math.Max(0, x.Stock)
                };
            })
            .Where(x => x.Key1 != null)     // ✅ Key1 obligatoire
            .ToList();

        // ✅ dédoublonnage (Key2 null autorisé)
        var dedup = normalized
            .GroupBy(x => new { x.Key1, x.Key2 })
            .Select(g => new ProductVariant
            {
                ProductId = id,

                // legacy champs
                Size = NormUnique(g.Key.Key1),
                Color = g.Key.Key2 == null ? "Unique" : g.Key.Key2,

                Stock = g.Sum(z => z.Stock),

                // ✅ champs indexés
                Key1 = NormUnique(g.Key.Key1),
                Key2 = g.Key.Key2 == null ? null : g.Key.Key2.Trim()
            })
            .ToList();

        var olds = await _db.ProductVariants.Where(v => v.ProductId == id).ToListAsync();
        _db.ProductVariants.RemoveRange(olds);

        _db.ProductVariants.AddRange(dedup);

        product.Stock = dedup.Sum(x => x.Stock);
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { ok = true, count = dedup.Count, stock = product.Stock });
    }


    public class SaveProductAttributesRequest
    {
        public List<SaveProductAttributeItem> Items { get; set; } = new();
    }
    public class SaveProductAttributeItem
    {
        public int AttributeId { get; set; }
        public int? OptionId { get; set; }
        public string? ValueText { get; set; }
        public int? ValueInt { get; set; }
        public decimal? ValueDecimal { get; set; }
        public bool? ValueBool { get; set; }
        public DateTime? ValueDate { get; set; }
    }

    [HttpPost("{id:int}/attributes")]
    public async Task<IActionResult> SaveAttributes(int id, [FromBody] SaveProductAttributesRequest req)
    {
        if (!await HasPermission("products.manage"))
            return Forbid();

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound(new { ok = false, message = "Produit introuvable." });

        var olds = await _db.ProductAttributeValues.Where(x => x.ProductId == id).ToListAsync();
        _db.ProductAttributeValues.RemoveRange(olds);

        foreach (var it in (req?.Items ?? new()))
        {
            if (it.AttributeId <= 0) continue;

            _db.ProductAttributeValues.Add(new ProductAttributeValue
            {
                ProductId = id,
                ProductVariantId = null,
                AttributeId = it.AttributeId,
                OptionId = it.OptionId,
                ValueText = it.ValueText,
                ValueInt = it.ValueInt,
                ValueDecimal = it.ValueDecimal,
                ValueBool = it.ValueBool,
                ValueDate = it.ValueDate
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }


    public record DuplicateProductDto(string Sku, string? Name = null);

    [HttpPost("{id:int}/duplicate")]
    public async Task<IActionResult> Duplicate(int id, [FromBody] DuplicateProductDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Sku))
            return BadRequest(new { ok = false, message = "SKU obligatoire." });

        var sku = dto.Sku.Trim().ToUpperInvariant();
        var exists = await _db.Products.AnyAsync(x => x.Sku == sku);
        if (exists) return BadRequest(new { ok = false, message = "SKU déjà utilisé." });

        var src = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (src == null) return NotFound(new { ok = false, message = "Produit introuvable." });

        var name = string.IsNullOrWhiteSpace(dto.Name) ? $"{src.Name} (Copie)" : dto.Name.Trim();
        var slugBase = SlugHelper.Slugify(name);
        var slug = await EnsureUniqueSlugAsync(slugBase);

        var p = new Product
        {
            Name = name,
            Slug = slug,
            CategoryId = src.CategoryId,
            IsActive = src.IsActive,
            CreatedAt = DateTime.UtcNow,

            Price = src.Price,
            PricePromo = src.PricePromo,
            Stock = src.Stock,
            Brand = src.Brand,
            Sku = sku,

            ShortDescription = src.ShortDescription,
            Description = src.Description,
            LongDescription = src.LongDescription,
            Highlights = src.Highlights,
            WeightKg = src.WeightKg,
            Dimensions = src.Dimensions
        };

        _db.Products.Add(p);
        await _db.SaveChangesAsync();

        // ✅ Attributes
        var attrs = await _db.ProductAttributeValues.Where(x => x.ProductId == id).ToListAsync();
        foreach (var a in attrs)
        {
            _db.ProductAttributeValues.Add(new ProductAttributeValue
            {
                ProductId = p.Id,
                ProductVariantId = null,
                AttributeId = a.AttributeId,
                OptionId = a.OptionId,
                ValueText = a.ValueText,
                ValueInt = a.ValueInt,
                ValueDecimal = a.ValueDecimal,
                ValueBool = a.ValueBool,
                ValueDate = a.ValueDate
            });
        }

        // ✅ Variants
        foreach (var v in src.Variants)
        {
            _db.ProductVariants.Add(new ProductVariant
            {
                ProductId = p.Id,
                Size = v.Size,
                Color = v.Color,
                Stock = v.Stock
            });
        }

        // ✅ Images : copier physiquement les fichiers vers le nouveau produit
        var srcDir = Path.Combine(_env.ContentRootPath, "uploads", "products", src.Id.ToString());
        var dstDir = Path.Combine(_env.ContentRootPath, "uploads", "products", p.Id.ToString());
        Directory.CreateDirectory(dstDir);

        bool anyMain = false;

        foreach (var img in src.Images.OrderBy(x => x.SortOrder))
        {
            if (string.IsNullOrWhiteSpace(img.Url)) continue;

            if (!img.Url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                continue;

            var rel = img.Url.TrimStart('/')
                .Replace("/", Path.DirectorySeparatorChar.ToString());

            var srcPath = Path.Combine(_env.ContentRootPath, rel);

            if (!System.IO.File.Exists(srcPath))
                continue;

            var ext = Path.GetExtension(srcPath);
            var newFileName = $"{Guid.NewGuid():N}{ext}";
            var dstPath = Path.Combine(dstDir, newFileName);

            System.IO.File.Copy(srcPath, dstPath, true);

            var newUrl = $"/uploads/products/{p.Id}/{newFileName}";

            var newImg = new ProductImage
            {
                ProductId = p.Id,
                Url = newUrl,
                IsMain = img.IsMain,
                SortOrder = img.SortOrder
            };

            if (newImg.IsMain) anyMain = true;

            _db.ProductImages.Add(newImg);
        }

        // sécurité : si aucune image principale
        if (!anyMain)
        {
            var first = await _db.ProductImages
                .Where(x => x.ProductId == p.Id)
                .OrderBy(x => x.SortOrder)
                .FirstOrDefaultAsync();

            if (first != null)
                first.IsMain = true;
        }


        await _db.SaveChangesAsync();

        return Ok(new { ok = true, id = p.Id });
    }

    // ============================
    // PUBLISH PRODUCT (ADMIN)
    // ============================
    [HttpPost("{id:int}/publish")]
    public async Task<IActionResult> Publish(int id)
    {

        if (!await HasPermission("products.moderate"))
            return Forbid();

        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (p == null)
            return NotFound(new { ok = false, message = "Produit introuvable." });

        p.PublishedStatus = "Published";
        p.IsActive = true;

        // ✅ nettoyer motif si on republie
        p.RejectReason = null;
        p.RejectedAt = null;

        p.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { ok = true, status = p.PublishedStatus });
    }

    // ============================
    // REJECT PRODUCT (ADMIN)
    // ============================
    public record RejectProductDto(string? Reason);

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectProductDto? dto)
    {

        if (!await HasPermission("products.moderate"))
            return Forbid();

        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (p == null)
            return NotFound(new { ok = false, message = "Produit introuvable." });

        var reason = (dto?.Reason ?? "").Trim();
        if (reason.Length > 500) reason = reason[..500]; // sécurité

        p.PublishedStatus = "Rejected";
        p.IsActive = false;

        // ✅ IMPORTANT
        p.RejectReason = string.IsNullOrWhiteSpace(reason) ? null : reason;
        p.RejectedAt = DateTime.UtcNow;

        p.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { ok = true, status = p.PublishedStatus });
    }


    // ============================


    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {

        if (!await HasPermission("products.moderate"))
            return Forbid();

        var items = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => !p.IsDeleted && p.PublishedStatus == "Pending")
            .OrderByDescending(p => p.Id)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Price,
                p.Stock,
                p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : null,
                p.VendorId,

                vendorName = p.Vendor != null ? p.Vendor.Name : null, // ✅
                submittedAt = p.CreatedAt,                             // ✅

                mainImageUrl = p.Images
                    .OrderByDescending(i => i.IsMain)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => i.Url)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(new { ok = true, items });
    }

    // GET /api/admin/products/moderation?status=Pending|Published|Rejected|Deleted
    [HttpGet("moderation")]
    public async Task<IActionResult> Moderation([FromQuery] string status = "Pending", [FromQuery] string? stock = null)
    {

        if (!await HasPermission("products.moderate"))
            return Forbid();

        status = (status ?? "Pending").Trim();

        IQueryable<Product> q = _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Vendor); // ✅ AJOUT

        // Deleted = IsDeleted=true
        if (status.Equals("Deleted", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(p => p.IsDeleted);
        }
        else
        {
            q = q.Where(p => !p.IsDeleted && p.PublishedStatus == status);
        }

        if (string.Equals(stock, "low", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(p =>
                (p.Variants.Any() ? p.Variants.Sum(v => v.Stock) : p.Stock) <= 5
            );
        }


        var items = await q
            .OrderByDescending(p => p.Id)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Price,
                p.Stock,
                p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : null,
                p.VendorId,

                vendorName = p.Vendor != null ? p.Vendor.Name : null, // ✅
                submittedAt = p.CreatedAt,                             // ✅

                status = p.PublishedStatus,
                isDeleted = p.IsDeleted,
                mainImageUrl = p.Images
                    .OrderByDescending(i => i.IsMain)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => i.Url)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(new { ok = true, items });
    }


    // GET /api/admin/products/by-vendor?vendorId=1&status=Pending
    // GET /api/admin/products/by-vendor?vendorId=1&status=Pending|Published|Rejected|Deleted
    [HttpGet("by-vendor")]
    public async Task<IActionResult> ByVendor([FromQuery] int vendorId, [FromQuery] string? status)
    {
        if (vendorId <= 0)
            return BadRequest(new { ok = false, message = "vendorId invalide" });

        status = (status ?? "").Trim();

        var q = _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => p.VendorId == vendorId);

        // ✅ Deleted => uniquement IsDeleted = true
        if (status.Equals("Deleted", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(p => p.IsDeleted);
        }
        else
        {
            // ✅ tous les autres onglets => on exclut les supprimés
            q = q.Where(p => !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(status))
            {
                q = q.Where(p => p.PublishedStatus != null &&
                                 p.PublishedStatus.ToLower() == status.ToLower());
            }
        }

        var items = await q
            .OrderByDescending(p => p.Id)
            .Select(p => new
            {
                p.Id,
                name = p.Name,
                price = p.Price,
                stock = p.Variants.Any() ? p.Variants.Sum(v => v.Stock) : p.Stock,
                categoryName = p.Category != null ? p.Category.Name : null,
                status = p.IsDeleted ? "Deleted" : p.PublishedStatus,
                isDeleted = p.IsDeleted,
                mainImageUrl = p.Images
                    .OrderByDescending(i => i.IsMain)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => i.Url)
                    .FirstOrDefault(),
                p.VendorId
            })
            .ToListAsync();

        return Ok(new { ok = true, items });
    }

    private async Task<bool> HasPermission(string code)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
            return false;

        var user = await _db.Users
            .Include(u => u.RoleRef)!
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        if (user == null || user.RoleRef == null)
            return false;

        if (string.Equals(user.RoleRef.Code, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase))
            return true;

        return user.RoleRef.RolePermissions.Any(rp => rp.Permission.Code == code);
    }
}