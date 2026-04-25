using EcommerceApi.Data;
using EcommerceApi.Helpers;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/vendor/products")]
[Authorize(Roles = "Vendor")]
public class VendorProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly PushService _push;
    public VendorProductsController(AppDbContext db, IWebHostEnvironment env, PushService push)
    {
        _db = db;
        _env = env;
        _push = push;
    }

    // -----------------------
    // DTOs
    // -----------------------
    public record VendorProductListItemDto(
        int Id,
        string Name,
        string Slug,
        decimal Price,
        int Stock,
        bool IsActive,
        bool IsDeleted,
        string PublishedStatus,
        int CategoryId,
        string? Sku,
        string? MainImageUrl,
        DateTime CreatedAt,
        string? RejectReason,     // ✅
        DateTime? RejectedAt
    );

    public record CreateVendorProductDto(
        string Name,
        decimal Price,
        decimal? PricePromo,
        int CategoryId,
        int Stock,
        string? Brand,
        string? Sku,
        string? ShortDescription,
        string? Description,
        string? Highlights
    );

    public record UpdateVendorProductDto(
        string Name,
        decimal Price,
        decimal? PricePromo,
        int CategoryId,
        int Stock,
        string? Brand,
        string? Sku,
        string? ShortDescription,
        string? Description,
        string? Highlights
    );

    // -----------------------
    // Helpers
    // -----------------------
    private bool TryGetVendorId(out int vendorId)
    {
        vendorId = 0;
        var vendorIdStr = User.FindFirstValue("vendorId");
        return int.TryParse(vendorIdStr, out vendorId);
    }

    private async Task<Vendor?> GetVendorOrNull(int vendorId)
        => await _db.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId);

    // -----------------------
    // GET /api/vendor/products
    // -----------------------
    [HttpGet]
    public async Task<IActionResult> GetMyProducts(
        [FromQuery] string? status = null,
        [FromQuery] bool includeDeleted = false
    )
    {
        if (!TryGetVendorId(out var vendorId))
            return Unauthorized(new { ok = false, message = "vendorId manquant." });

        var q = _db.Products.AsNoTracking()
     .Include(p => p.Images)
     .Include(p => p.Variants)
     .Where(p => p.VendorId == vendorId);

        if (!includeDeleted)
            q = q.Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var st = status.Trim();
            q = q.Where(p => p.PublishedStatus == st);
        }

        var items = await q
            .OrderByDescending(p => p.Id)
            .Select(p => new VendorProductListItemDto(
                p.Id,
                p.Name,
                p.Slug,
                p.Price,
                p.Variants.Any() ? p.Variants.Sum(v => v.Stock) : p.Stock,
                p.IsActive,
                p.IsDeleted,
                p.PublishedStatus,
                p.CategoryId,
                p.Sku,
                p.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                p.CreatedAt,

                p.RejectReason,
                p.RejectedAt
            ))
            .ToListAsync();

        return Ok(new { ok = true, items });
    }

    // -----------------------
    // POST /api/vendor/products
    // (Create => Pending)
    // -----------------------
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVendorProductDto dto)
    {
        if (!TryGetVendorId(out var vendorId))
            return Unauthorized(new { ok = false, message = "vendorId manquant." });

        var vendor = await GetVendorOrNull(vendorId);
        if (vendor is null)
            return Unauthorized(new { ok = false, message = "Boutique introuvable." });

        if (vendor.Status != 1)
            return Forbid(); // En attente ou bloqué

        var name = (dto.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { ok = false, message = "Nom obligatoire." });

        if (dto.Price <= 0)
            return BadRequest(new { ok = false, message = "Prix invalide." });

        if (dto.CategoryId <= 0)
            return BadRequest(new { ok = false, message = "Catégorie obligatoire." });

        // SKU (optionnel côté vendeur, mais si tu veux l'obliger, mets le check ici)
        var sku = (dto.Sku ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(sku))
        {
            sku = sku.ToUpperInvariant();
            var existsSku = await _db.Products.AnyAsync(x => x.Sku == sku);
            if (existsSku) return BadRequest(new { ok = false, message = "SKU déjà utilisé." });
        }

        var slugBase = SlugHelper.Slugify(name);
        var slug = slugBase;

        // slug unique simple
        var n = 1;
        while (await _db.Products.AnyAsync(p => p.Slug == slug))
        {
            n++;
            slug = $"{slugBase}-{n}";
        }

        var p = new Product
        {
            VendorId = vendorId,
            Name = name,
            Slug = slug,
            Price = dto.Price,
            PricePromo = dto.PricePromo,
            Stock = Math.Max(0, dto.Stock),
            CategoryId = dto.CategoryId,

            Brand = string.IsNullOrWhiteSpace(dto.Brand) ? null : dto.Brand.Trim(),
            Sku = string.IsNullOrWhiteSpace(sku) ? null : sku,

            ShortDescription = string.IsNullOrWhiteSpace(dto.ShortDescription) ? null : dto.ShortDescription.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            Highlights = string.IsNullOrWhiteSpace(dto.Highlights) ? null : dto.Highlights.Trim(),

            LongDescription = null,
            PublishedStatus = "Pending",
            IsActive = false,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Products.Add(p);
        await _db.SaveChangesAsync();

        _db.AdminNotifications.Add(new AdminNotification
        {
            Type = "VendorProduct",
            RefId = p.Id,
            Title = "Nouveau produit à valider",
            Message = p.Name,
            Body = $"Boutique {vendor.Name} a soumis un produit.",
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        // ✅ UTILISER vendor.Id (PAS vendorId)
        await _push.SendNewVendorProductToAdminsAsync(p.Id, p.Name, vendor.Name);
        return Ok(new { ok = true, id = p.Id, status = p.PublishedStatus });
    }

    // -----------------------
    // PUT /api/vendor/products/{id}
    // (Update => repasse Pending si déjà Rejected/Published)
    // -----------------------
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVendorProductDto dto)
    {
        if (!TryGetVendorId(out var vendorId))
            return Unauthorized(new { ok = false, message = "vendorId manquant." });

        var vendor = await GetVendorOrNull(vendorId);
        if (vendor is null) return Unauthorized(new { ok = false, message = "Boutique introuvable." });
        if (vendor.Status != 1) return Forbid();

        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id && x.VendorId == vendorId);
        if (p == null) return NotFound(new { ok = false, message = "Produit introuvable." });

        if (p.IsDeleted) return BadRequest(new { ok = false, message = "Produit supprimé." });

        var name = (dto.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { ok = false, message = "Nom obligatoire." });

        if (dto.Price <= 0)
            return BadRequest(new { ok = false, message = "Prix invalide." });

        if (dto.CategoryId <= 0)
            return BadRequest(new { ok = false, message = "Catégorie obligatoire." });

        // SKU optionnel update (unicité)
        var skuRaw = (dto.Sku ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(skuRaw))
        {
            var sku = skuRaw.ToUpperInvariant();
            var existsSku = await _db.Products.AnyAsync(x => x.Id != id && x.Sku == sku);
            if (existsSku) return BadRequest(new { ok = false, message = "SKU déjà utilisé." });
            p.Sku = sku;
        }

        // si nom change => slug unique
        if (!string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
        {
            var slugBase = SlugHelper.Slugify(name);
            var slug = slugBase;
            var n = 1;
            while (await _db.Products.AnyAsync(x => x.Id != id && x.Slug == slug))
            {
                n++;
                slug = $"{slugBase}-{n}";
            }
            p.Slug = slug;
            p.Name = name;
        }

        p.Price = dto.Price;
        p.PricePromo = dto.PricePromo;
        p.Stock = Math.Max(0, dto.Stock);
        p.CategoryId = dto.CategoryId;
        p.Brand = string.IsNullOrWhiteSpace(dto.Brand) ? null : dto.Brand.Trim();
        p.ShortDescription = string.IsNullOrWhiteSpace(dto.ShortDescription) ? null : dto.ShortDescription.Trim();
        p.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        p.Highlights = string.IsNullOrWhiteSpace(dto.Highlights) ? null : dto.Highlights.Trim();

        // ✅ important marketplace : si le vendeur modifie, on repasse en Pending
        p.PublishedStatus = "Pending";
        p.IsActive = false;
        p.UpdatedAt = DateTime.UtcNow;

        p.ShortDescription = string.IsNullOrWhiteSpace(dto.ShortDescription) ? null : dto.ShortDescription.Trim();
        p.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        p.Highlights = string.IsNullOrWhiteSpace(dto.Highlights) ? null : dto.Highlights.Trim();
        p.LongDescription = null; // vendor ne gère pas

        await _db.SaveChangesAsync();
        return Ok(new { ok = true, status = p.PublishedStatus });
    }

    // -----------------------
    // DELETE /api/vendor/products/{id}
    // (soft delete)
    // -----------------------
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetVendorId(out var vendorId))
            return Unauthorized(new { ok = false, message = "vendorId manquant." });

        var vendor = await GetVendorOrNull(vendorId);
        if (vendor is null) return Unauthorized(new { ok = false, message = "Boutique introuvable." });
        if (vendor.Status != 1) return Forbid();

        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id && x.VendorId == vendorId);
        if (p == null) return NotFound(new { ok = false, message = "Produit introuvable." });

        p.IsDeleted = true;
        p.IsActive = false;
        p.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    // =======================================================
    // VARIANTS
    // POST /api/vendor/products/{id}/variants
    // =======================================================
    public record UpsertVariantsDto(List<VariantItemDto> Items);
    public record VariantItemDto(string Key1, string Key2, int Stock);

    [HttpPost("{id:int}/variants")]
    public async Task<IActionResult> UpsertVariants(int id, [FromBody] UpsertVariantsDto dto)
    {
        if (!TryGetVendorId(out var vendorId))
            return Unauthorized(new { ok = false, message = "vendorId manquant." });

        var p = await _db.Products
            .Include(x => x.Variants)
            .FirstOrDefaultAsync(x => x.Id == id && x.VendorId == vendorId);

        if (p is null) return NotFound(new { ok = false, message = "Produit introuvable." });
        if (p.IsDeleted) return BadRequest(new { ok = false, message = "Produit supprimé." });

        var items = dto?.Items ?? new();
        if (!items.Any())
        {
            // Si on envoie vide => on supprime toutes les variantes (mode simple)
            if (p.Variants is not null && p.Variants.Any())
            {
                _db.ProductVariants.RemoveRange(p.Variants); // ⚠️ adapte si DbSet différent
                await _db.SaveChangesAsync();
            }

            // Stock redevient le stock du produit (déjà existant)
            return Ok(new { ok = true, variants = 0, stock = p.Stock });
        }

        // Normalisation + anti doublons
        var normalized = items
            .Select(v => new VariantItemDto(
                (v.Key1 ?? "").Trim(),
                string.IsNullOrWhiteSpace(v.Key2) ? null : v.Key2.Trim(),  // ✅ null si vide
                Math.Max(0, v.Stock)))
            .Where(v => !string.IsNullOrWhiteSpace(v.Key1))
            .ToList();

        if (!normalized.Any())
            return BadRequest(new { ok = false, message = "Variantes invalides." });


        var dup = normalized
            .GroupBy(v => (v.Key1.ToLowerInvariant(), (v.Key2 ?? "").ToLowerInvariant()))
            .Any(g => g.Count() > 1);

        if (dup) return BadRequest(new { ok = false, message = "Doublons dans les variantes." });

        // Remplacement complet (simple & fiable)
        if (p.Variants is not null && p.Variants.Any())
            _db.ProductVariants.RemoveRange(p.Variants); // ⚠️ adapte si DbSet différent

        foreach (var v in normalized)
        {
            p.Variants.Add(new ProductVariant
            {
                ProductId = p.Id,

                // ✅ important pour l’API publique (client)
                Size = v.Key1,
                Color = v.Key2,      // null si 1D

                Key1 = v.Key1,
                Key2 = v.Key2,
                Stock = v.Stock,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Stock produit = somme variantes (optionnel mais recommandé)
        p.Stock = normalized.Sum(x => x.Stock);
        p.UpdatedAt = DateTime.UtcNow;

        // Après modification on repasse en Pending
        p.PublishedStatus = "Pending";
        p.IsActive = false;

        await _db.SaveChangesAsync();
        return Ok(new { ok = true, variants = normalized.Count, stock = p.Stock });
    }


    // =======================================================
    // ATTRIBUTES (fiche produit)
    // POST /api/vendor/products/{id}/attributes
    // =======================================================
    public record UpsertProductAttributesDto(List<ProductAttrItemDto> Items);
    public record ProductAttrItemDto(int AttributeId, int? OptionId, string? ValueText);

    [HttpPost("{id:int}/attributes")]
    public async Task<IActionResult> UpsertAttributes(int id, [FromBody] UpsertProductAttributesDto dto)
    {
        if (!TryGetVendorId(out var vendorId))
            return Unauthorized(new { ok = false, message = "vendorId manquant." });

        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id && x.VendorId == vendorId);
        if (p is null) return NotFound(new { ok = false, message = "Produit introuvable." });
        if (p.IsDeleted) return BadRequest(new { ok = false, message = "Produit supprimé." });

        var items = dto?.Items ?? new();

        // On remplace tout (simple)
        // ⚠️ adapte DbSet + Entity si ton nom diffère (ProductAttributeValue, ProductAttribute, etc.)
        var existing = await _db.ProductAttributeValues
            .Where(x => x.ProductId == id)
            .ToListAsync();

        if (existing.Any())
            _db.ProductAttributeValues.RemoveRange(existing);

        // Nettoyage + éviter “rien”
        var cleaned = items
            .Select(x => new ProductAttrItemDto(
                x.AttributeId,
                x.OptionId is > 0 ? x.OptionId : null,
                string.IsNullOrWhiteSpace(x.ValueText) ? null : x.ValueText.Trim()))
            .Where(x => x.AttributeId > 0 && (x.OptionId != null || x.ValueText != null))
            .ToList();

        foreach (var a in cleaned)
        {
            _db.ProductAttributeValues.Add(new ProductAttributeValue
            {
                ProductId = id,
                AttributeId = a.AttributeId,
                OptionId = a.OptionId,
                ValueText = a.ValueText,
                CreatedAt = DateTime.UtcNow
            });
        }

        p.UpdatedAt = DateTime.UtcNow;

        // Après modification on repasse en Pending
        p.PublishedStatus = "Pending";
        p.IsActive = false;

        await _db.SaveChangesAsync();
        return Ok(new { ok = true, count = cleaned.Count });
    }


    // =======================================================
    // IMAGES
    // POST /api/vendor/products/{id}/images  (multipart/form-data)
    // field: files (multiple)
    // =======================================================
    [HttpPost("{id:int}/images")]
    [RequestSizeLimit(25_000_000)] // 25 MB (ajuste)
    public async Task<IActionResult> UploadImages(
        int id,
        [FromForm] List<IFormFile> files,
        [FromForm] bool setFirstAsMain = true
    )
    {
        if (!TryGetVendorId(out var vendorId))
            return Unauthorized(new { ok = false, message = "vendorId manquant." });

        var p = await _db.Products
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id && x.VendorId == vendorId);

        if (p is null) return NotFound(new { ok = false, message = "Produit introuvable." });
        if (p.IsDeleted) return BadRequest(new { ok = false, message = "Produit supprimé." });

        if (files == null || files.Count == 0)
            return BadRequest(new { ok = false, message = "Aucun fichier reçu." });

        // Limite count (ex: 6)
        var currentCount = p.Images?.Count ?? 0;
        var max = 6;
        if (currentCount >= max) return BadRequest(new { ok = false, message = $"Maximum {max} images." });

        var remaining = max - currentCount;
        var toSave = files.Take(remaining).ToList();

        // Dossier: wwwroot/uploads/products/{vendorId}/{productId}
        var relFolder = Path.Combine("uploads", "products", vendorId.ToString(), id.ToString());
        var absFolder = Path.Combine(_env.WebRootPath, relFolder);
        Directory.CreateDirectory(absFolder);

        int sort = (p.Images?.Any() == true) ? p.Images.Max(i => i.SortOrder) + 1 : 1;

        var saved = new List<string>();

        foreach (var f in toSave)
        {
            if (f.Length <= 0) continue;

            var ext = Path.GetExtension(f.FileName)?.ToLowerInvariant();
            var allowed = new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(ext))
                return BadRequest(new { ok = false, message = "Format image non supporté (jpg/png/webp)." });

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var absPath = Path.Combine(absFolder, fileName);

            using (var stream = System.IO.File.Create(absPath))
                await f.CopyToAsync(stream);

            var url = "/" + relFolder.Replace("\\", "/") + "/" + fileName;

            p.Images.Add(new ProductImage // ⚠️ adapte si besoin
            {
                ProductId = id,
                Url = url,
                SortOrder = sort++,
                IsMain = false,
                CreatedAt = DateTime.UtcNow
            });

            saved.Add(url);
        }

        // Main image
        if (setFirstAsMain && saved.Count > 0)
        {
            foreach (var img in p.Images) img.IsMain = false;
            var first = p.Images.OrderBy(i => i.SortOrder).FirstOrDefault();
            if (first != null) first.IsMain = true;
        }
        else if (p.Images != null && p.Images.All(i => i.IsMain == false))
        {
            var first = p.Images.OrderBy(i => i.SortOrder).FirstOrDefault();
            if (first != null) first.IsMain = true;
        }

        // Après upload => Pending aussi (pro)
        p.PublishedStatus = "Pending";
        p.IsActive = false;
        p.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { ok = true, added = saved.Count, urls = saved });
    }



    // =======================================================
    // DUPLICATE PRO (Produit + Images + Variants + Attributes)
    // POST /api/vendor/products/{id}/duplicate
    // =======================================================
    [HttpPost("{id:int}/duplicate")]
    public async Task<IActionResult> DuplicatePro(int id)
    {
        if (!TryGetVendorId(out var vendorId))
            return Unauthorized(new { ok = false, message = "vendorId manquant." });

        var vendor = await GetVendorOrNull(vendorId);
        if (vendor is null) return Unauthorized(new { ok = false, message = "Boutique introuvable." });
        if (vendor.Status != 1) return Forbid();

        // ✅ Source produit + relations
        var src = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id && p.VendorId == vendorId);

        if (src is null) return NotFound(new { ok = false, message = "Produit introuvable." });
        if (src.IsDeleted) return BadRequest(new { ok = false, message = "Produit supprimé." });

        // ✅ Attributs (table séparée)
        var srcAttrs = await _db.ProductAttributeValues
            .AsNoTracking()
            .Where(x => x.ProductId == id)
            .ToListAsync();

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // -----------------------
            // 1) Nouveau produit
            // -----------------------
            var newName = ((src.Name ?? "Produit").Trim()) + " (copie)";
            var slugBase = SlugHelper.Slugify(newName);
            var slug = slugBase;
            var n = 1;

            while (await _db.Products.AnyAsync(p => p.Slug == slug))
            {
                n++;
                slug = $"{slugBase}-{n}";
            }

            var copy = new Product
            {
                VendorId = vendorId,
                Name = newName,
                Slug = slug,

                Price = src.Price,
                Stock = src.Variants.Any() ? src.Variants.Sum(v => v.Stock) : src.Stock,
                CategoryId = src.CategoryId,

                Sku = null, // ✅ SKU unique
                ShortDescription = src.ShortDescription,
                Description = src.Description,
                Highlights = src.Highlights,
                LongDescription = null,
                PricePromo = src.PricePromo,
                Brand = src.Brand,

                PublishedStatus = "Pending",
                IsActive = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Products.Add(copy);
            await _db.SaveChangesAsync(); // ✅ copy.Id

            // -----------------------
            // 2) Variants
            // -----------------------
            if (src.Variants?.Any() == true)
            {
                foreach (var v in src.Variants)
                {
                    _db.ProductVariants.Add(new ProductVariant
                    {
                        ProductId = copy.Id,
                        Key1 = v.Key1,
                        Key2 = v.Key2,
                        Stock = v.Stock,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // -----------------------
            // 3) Attributes
            // -----------------------
            if (srcAttrs.Any())
            {
                foreach (var a in srcAttrs)
                {
                    _db.ProductAttributeValues.Add(new ProductAttributeValue
                    {
                        ProductId = copy.Id,
                        AttributeId = a.AttributeId,
                        OptionId = a.OptionId,
                        ValueText = a.ValueText,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // -----------------------
            // 4) Images (DB + copie fichiers)
            // -----------------------
            if (src.Images?.Any() == true)
            {
                // Dossier cible: wwwroot/uploads/products/{vendorId}/{newId}
                var relFolder = Path.Combine("uploads", "products", vendorId.ToString(), copy.Id.ToString());
                var absFolder = Path.Combine(_env.WebRootPath, relFolder);
                Directory.CreateDirectory(absFolder);

                foreach (var img in src.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.SortOrder))
                {
                    // img.Url exemple: /uploads/products/{vendorId}/{oldId}/xxx.jpg
                    var oldUrl = img.Url ?? "";
                    var newUrl = oldUrl;

                    // ✅ si c’est un fichier local dans wwwroot => on copie physiquement
                    if (!string.IsNullOrWhiteSpace(oldUrl) && oldUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                    {
                        var oldRel = oldUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
                        var oldAbs = Path.Combine(_env.WebRootPath, oldRel);

                        if (System.IO.File.Exists(oldAbs))
                        {
                            var ext = Path.GetExtension(oldAbs);
                            var newFile = $"{Guid.NewGuid():N}{ext}";
                            var newAbs = Path.Combine(absFolder, newFile);

                            System.IO.File.Copy(oldAbs, newAbs, overwrite: false);

                            newUrl = "/" + relFolder.Replace("\\", "/") + "/" + newFile;
                        }
                    }

                    _db.ProductImages.Add(new ProductImage
                    {
                        ProductId = copy.Id,
                        Url = newUrl,
                        SortOrder = img.SortOrder,
                        IsMain = img.IsMain,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // sécurité: s’il n’y a aucun main, on met le 1er
                // (au cas où source avait un mauvais état)
                // => on le fera après SaveChanges si besoin
            }

            await _db.SaveChangesAsync();

            // ✅ sécurité main image
            var newImgs = await _db.ProductImages.Where(i => i.ProductId == copy.Id).ToListAsync();
            if (newImgs.Any() && newImgs.All(i => !i.IsMain))
            {
                var first = newImgs.OrderBy(i => i.SortOrder).First();
                first.IsMain = true;
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();

            return Ok(new { ok = true, newId = copy.Id });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, new { ok = false, message = "Erreur duplication.", detail = ex.Message });
        }
    }



    // -----------------------
    // GET /api/vendor/products/{id}
    // Détails pour édition (produit + images + variants + attributes)
    // -----------------------
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOne(int id)
    {
        if (!TryGetVendorId(out var vendorId))
            return Unauthorized(new { ok = false, message = "vendorId manquant." });

        var p = await _db.Products
            .AsNoTracking()
            .Include(x => x.Images)
            .Include(x => x.Variants)
            .FirstOrDefaultAsync(x => x.Id == id && x.VendorId == vendorId);

        if (p is null) return NotFound(new { ok = false, message = "Produit introuvable." });
        if (p.IsDeleted) return BadRequest(new { ok = false, message = "Produit supprimé." });

        var attrs = await _db.ProductAttributeValues
            .AsNoTracking()
            .Where(a => a.ProductId == id)
            .Select(a => new
            {
                a.AttributeId,
                a.OptionId,
                a.ValueText
            })
            .ToListAsync();

        return Ok(new
        {
            ok = true,
            item = new
            {
                p.Id,
                p.Name,
                p.Slug,
                p.Price,
                p.PricePromo,
                p.Stock,
                p.CategoryId,
                p.Sku,
                p.ShortDescription,
                p.Description,
                p.Highlights,

                p.PublishedStatus,
                p.IsActive,

                // ✅ AJOUTS (motif rejet)
                p.RejectReason,
                p.RejectedAt,

                images = p.Images
                    .OrderByDescending(i => i.IsMain)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => new { i.Id, i.Url, i.IsMain, i.SortOrder })
                    .ToList(),

                variants = p.Variants
                    .OrderBy(v => v.Id)
                    .Select(v => new { v.Id, v.Key1, v.Key2, v.Stock })
                    .ToList(),

                attributes = attrs
            }
        });
    }

    // DELETE /api/vendor/products/{id}/images
    [HttpDelete("{id:int}/images")]
    public async Task<IActionResult> DeleteAllImages(int id)
    {
        if (!TryGetVendorId(out var vendorId))
            return Unauthorized();

        var p = await _db.Products
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id && x.VendorId == vendorId);

        if (p == null) return NotFound();

        _db.ProductImages.RemoveRange(p.Images);
        await _db.SaveChangesAsync();

        return Ok(new { ok = true });
    }


    // -----------------------
    // POST /api/vendor/products/{id}/restore
    // -----------------------
    [HttpPost("{id:int}/restore")]
    public async Task<IActionResult> Restore(int id)
    {
        if (!TryGetVendorId(out var vendorId))
            return Unauthorized(new { ok = false, message = "vendorId manquant." });

        var vendor = await GetVendorOrNull(vendorId);
        if (vendor is null)
            return Unauthorized(new { ok = false, message = "Boutique introuvable." });

        if (vendor.Status != 1)
            return Forbid();

        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id && x.VendorId == vendorId);
        if (p == null)
            return NotFound(new { ok = false, message = "Produit introuvable." });

        if (!p.IsDeleted)
            return BadRequest(new { ok = false, message = "Ce produit n'est pas supprimé." });

        p.IsDeleted = false;
        p.IsActive = false;
        p.PublishedStatus = "Pending";
        p.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { ok = true, message = "Produit restauré.", status = p.PublishedStatus });
    }


    public record DeleteImageByUrlDto(string Url);

    [HttpDelete("{id:int}/images/by-url")]
    public async Task<IActionResult> DeleteImageByUrl(int id, [FromBody] DeleteImageByUrlDto dto)
    {
        if (!TryGetVendorId(out var vendorId))
            return Unauthorized(new { ok = false, message = "vendorId manquant." });

        var p = await _db.Products
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id && x.VendorId == vendorId);

        if (p == null)
            return NotFound(new { ok = false, message = "Produit introuvable." });

        var url = (dto?.Url ?? "").Trim();
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest(new { ok = false, message = "URL image manquante." });

        var img = p.Images.FirstOrDefault(x => x.Url == url);
        if (img == null)
            return NotFound(new { ok = false, message = "Image introuvable." });

        _db.ProductImages.Remove(img);
        await _db.SaveChangesAsync();

        return Ok(new { ok = true });
    }
}
