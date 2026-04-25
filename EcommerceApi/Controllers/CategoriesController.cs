using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/admin/categories")]

[Authorize(Roles = "SUPER_ADMIN,ADMIN,MANAGER")]

public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    private readonly IWebHostEnvironment _env;

    public CategoriesController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }


    // ✅ PUBLIC: Liste
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Slug,
                c.ImageUrl,
                c.ParentId,
                c.SortOrder,
                c.IsActive,
                c.CommissionRate,
                c.MetaTitle,
                c.MetaDescription,
                c.Description
            })


            .ToListAsync();

        return Ok(new { ok = true, items = categories });
    }


    [HttpGet("tree")]
    public async Task<IActionResult> GetTree()
    {
        var items = await _db.Categories
            .AsNoTracking()
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Slug,
                x.ImageUrl,
                x.ParentId,
                x.SortOrder,
                x.IsActive,
                x.CommissionRate, // ✅ AJOUT
                x.MetaTitle,
                x.MetaDescription,
                x.Description
            })
            .ToListAsync();

        // ✅ sécurité: si slug null, on le remplace (ou on laisse mais sans casser)
        var safe = items.Select(x => new
        {
            x.Id,
            x.Name,
            Slug = string.IsNullOrWhiteSpace(x.Slug) ? Slugify(x.Name ?? $"cat-{x.Id}") : x.Slug,
            x.ImageUrl,
            x.ParentId,
            x.SortOrder,
            x.IsActive,
            x.CommissionRate, // ✅ AJOUT
            x.MetaTitle,
            x.MetaDescription,
            x.Description
        }).ToList();

        return Ok(new { ok = true, items = safe });
    }




    // ✅ PUBLIC: Détail
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOne(int id)
    {
        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (c == null)
            return NotFound(new { ok = false, message = "Catégorie introuvable." });

        return Ok(new
        {
            ok = true,
            item = new
            {
                c.Id,
                c.Name,
                c.Slug,
                c.ImageUrl,

                c.MetaTitle,
                c.MetaDescription,
                c.Description,


            }
        });
    }

    public class UpsertCategoryDto
    {
        public string Name { get; set; } = "";
        public string? Slug { get; set; }          // optionnel
        public string? ImageUrl { get; set; }      // optionnel si colonne existe
        public int? SortOrder { get; set; }        // optionnel si colonne existe
        public int? ParentId { get; set; }
        public bool IsActive { get; set; } = true;

        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? Description { get; set; }

    }

    private async Task<string> BuildSlugAsync(string input, int? parentId, int? excludeId = null)
    {
        var baseSlug = Slugify(input);
        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = "cat";

        var candidate = baseSlug;
        var n = 2;

        // ✅ IMPORTANT: slug unique GLOBAL (pas par ParentId),
        // car tu as un index unique IX_Categories_Slug
        while (await _db.Categories.AnyAsync(c =>
            c.Slug == candidate &&
            (excludeId == null || c.Id != excludeId.Value)))
        {
            candidate = $"{baseSlug}-{n}";
            n++;
        }

        return candidate;
    }







    private async Task<string> GetParentSlugPathAsync(int? parentId)
    {
        if (parentId == null) return "";

        var slugs = new List<string>();
        var currentId = parentId;

        // remonte la chaîne des parents
        while (currentId != null)
        {
            var p = await _db.Categories
                .Where(x => x.Id == currentId.Value)
                .Select(x => new { x.ParentId, x.Slug })
                .FirstOrDefaultAsync();

            if (p == null || string.IsNullOrWhiteSpace(p.Slug))
                break;

            slugs.Add(p.Slug);
            currentId = p.ParentId;
        }

        // enfant -> mode devient "enfant-mode"
        return string.Join("-", slugs);
    }



    // ✅ ADMIN: Create

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertCategoryDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { ok = false, message = "Nom obligatoire." });

        var input = string.IsNullOrWhiteSpace(dto.Slug) ? dto.Name : dto.Slug!;
        var slug = await BuildSlugAsync(input, dto.ParentId);

        int sortOrder;
        if (dto.SortOrder.HasValue) sortOrder = dto.SortOrder.Value;
        else
        {
            var max = await _db.Categories
                .Where(x => x.ParentId == dto.ParentId)
                .MaxAsync(x => (int?)x.SortOrder) ?? -1;
            sortOrder = max + 1;
        }




        string? parentName = null;
        if (dto.ParentId != null)
        {
            parentName = await _db.Categories
                .Where(x => x.Id == dto.ParentId.Value)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();
        }



        var cat = new Category
        {
            Name = dto.Name.Trim(),
            Slug = slug,
            ParentId = dto.ParentId,
            SortOrder = sortOrder,
            IsActive = dto.IsActive,



            // ✅ SEO (auto si vide)
            MetaTitle = string.IsNullOrWhiteSpace(dto.MetaTitle)
        ? AutoMetaTitle(dto.Name, parentName)
        : dto.MetaTitle.Trim(),

            MetaDescription = string.IsNullOrWhiteSpace(dto.MetaDescription)
        ? AutoMetaDescription(dto.Name)
        : dto.MetaDescription.Trim(),

            Description = string.IsNullOrWhiteSpace(dto.Description)
        ? null
        : dto.Description.Trim(),
        };


        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            ok = true,
            item = new
            {
                cat.Id,
                cat.Name,
                cat.Slug,
                cat.ImageUrl,
                cat.ParentId,
                cat.SortOrder,
                cat.IsActive,
                cat.CommissionRate, // ✅ AJOUT
                cat.MetaTitle,
                cat.MetaDescription,
                cat.Description
            }
        });

    }



    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertCategoryDto dto)
    {
        var cat = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (cat == null)
            return NotFound(new { ok = false, message = "Catégorie introuvable." });

        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { ok = false, message = "Nom obligatoire." });

        var input = string.IsNullOrWhiteSpace(dto.Slug) ? dto.Name : dto.Slug!;
        var slug = await BuildSlugAsync(input, dto.ParentId, excludeId: id);

        cat.Name = dto.Name.Trim();
        cat.Slug = slug;
        cat.ParentId = dto.ParentId;
        cat.IsActive = dto.IsActive;

        if (dto.SortOrder.HasValue) cat.SortOrder = dto.SortOrder.Value;






        // ✅ SEO : auto si vide + parentName
        string? parentName = null;
        if (dto.ParentId != null)
        {
            parentName = await _db.Categories
                .Where(x => x.Id == dto.ParentId.Value)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();
        }

        cat.MetaTitle = string.IsNullOrWhiteSpace(dto.MetaTitle)
            ? AutoMetaTitle(dto.Name, parentName)
            : dto.MetaTitle.Trim();

        cat.MetaDescription = string.IsNullOrWhiteSpace(dto.MetaDescription)
            ? AutoMetaDescription(dto.Name)
            : dto.MetaDescription.Trim();

        cat.Description = string.IsNullOrWhiteSpace(dto.Description)
            ? null
            : dto.Description.Trim();

        await _db.SaveChangesAsync();
        return Ok(new
        {
            ok = true,
            item = new
            {
                cat.Id,
                cat.Name,
                cat.Slug,
                cat.ImageUrl,
                cat.ParentId,
                cat.SortOrder,
                cat.IsActive,
                cat.CommissionRate, // ✅ AJOUT
                cat.MetaTitle,
                cat.MetaDescription,
                cat.Description
            }
        });

    }




    // ✅ ADMIN: Delete (avec protection si produits liés)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (cat == null)
            return NotFound(new { ok = false, message = "Catégorie introuvable." });

        // ✅ 1) Interdire si la catégorie a des enfants
        var hasChildren = await _db.Categories.AnyAsync(c => c.ParentId == id);
        if (hasChildren)
            return BadRequest(new { ok = false, message = "Impossible : cette catégorie contient des sous-catégories." });

        // ✅ 2) Interdire si des produits utilisent cette catégorie
        var hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == id);
        if (hasProducts)
            return BadRequest(new { ok = false, message = "Impossible : des produits utilisent cette catégorie." });

        var oldUrl = cat.ImageUrl;

        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();

        DeleteCategoryImageFile(oldUrl);

        return Ok(new { ok = true });
    }



    // ---------------- HELPERS ----------------
    private static string Slugify(string input)
    {
        input = (input ?? "").Trim().ToLowerInvariant();

        // ✅ retire accents
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        var s = sb.ToString().Normalize(NormalizationForm.FormC);

        // ✅ caractères autorisés: a-z 0-9 -> sinon "-"
        var outSb = new StringBuilder();
        foreach (var ch in s)
        {
            if (char.IsLetterOrDigit(ch)) outSb.Append(ch);
            else if (ch == ' ' || ch == '-' || ch == '_') outSb.Append('-');
        }

        var slug = outSb.ToString();
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }



    [HttpPost("{id:int}/image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage(int id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { ok = false, message = "Fichier manquant." });

        var cat = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (cat == null)
            return NotFound(new { ok = false, message = "Catégorie introuvable." });

        var oldUrl = cat.ImageUrl;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowed.Contains(ext))
            return BadRequest(new { ok = false, message = "Format non supporté (jpg, png, webp)." });

        var folder = Path.Combine(_env.WebRootPath, "uploads", "categories");
        Directory.CreateDirectory(folder);

        var fileName = $"cat_{id}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
        var fullPath = Path.Combine(folder, fileName);

        using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream);

        var url = $"/uploads/categories/{fileName}";
        cat.ImageUrl = url;

        await _db.SaveChangesAsync();

        DeleteCategoryImageFile(oldUrl);

        return Ok(new { ok = true, imageUrl = url });
    }


    private void DeleteCategoryImageFile(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return;

        // On supprime uniquement les fichiers locaux de /uploads/categories/
        if (!imageUrl.StartsWith("/uploads/categories/", StringComparison.OrdinalIgnoreCase))
            return;

        // Ton upload utilise wwwroot/uploads/categories
        var relative = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar); // uploads\categories\...
        var fullPath = Path.Combine(_env.WebRootPath, relative);

        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);
    }

    [HttpDelete("{id:int}/image")]
    public async Task<IActionResult> DeleteImage(int id)
    {
        var cat = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (cat == null)
            return NotFound(new { ok = false, message = "Catégorie introuvable." });

        var oldUrl = cat.ImageUrl;

        cat.ImageUrl = null;
        await _db.SaveChangesAsync();

        DeleteCategoryImageFile(oldUrl);

        return Ok(new { ok = true });
    }

    private static readonly HashSet<string> AllowedVariantModes = new(StringComparer.OrdinalIgnoreCase)
{
    "Simple", "Shoes", "Clothes", "Watch", "Phone", "Pack",
    "Appliance", "PackColor",
    "Belt", "ColorOnly",
    "Laptop" // ✅ AJOUT
};




    private static string NormalizeVariantMode(string? mode)
    {
        var v = (mode ?? "").Trim();
        if (string.IsNullOrWhiteSpace(v)) return "Simple";

        // ✅ retourne la valeur canonique (ex: "PackColor") au lieu de "Packcolor"
        foreach (var m in AllowedVariantModes)
            if (string.Equals(m, v, StringComparison.OrdinalIgnoreCase))
                return m;

        return "Simple";
    }


    private static string AutoMetaTitle(string name, string? parentName)
    {
        name = (name ?? "").Trim();
        parentName = (parentName ?? "").Trim();

        // Sous-catégorie : "Homme - Mode | Ranita"
        if (!string.IsNullOrWhiteSpace(parentName))
            return $"{name} - {parentName} | Ranita";

        // Racine : "Mode | Ranita"
        return $"{name} | Ranita";
    }

    private static string AutoMetaDescription(string name)
    {
        name = (name ?? "").Trim();
        return $"Découvrez {name} sur Ranita : meilleurs prix, articles de qualité et livraison rapide.";
    }

    [HttpGet("{parentId:int}/children")]
    public async Task<IActionResult> GetChildren(int parentId)
    {
        var children = await _db.Categories
            .Where(c => c.ParentId == parentId && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Slug,
                c.ImageUrl
            })
            .ToListAsync();

        return Ok(new { ok = true, items = children });
    }

    // =========================
    // GET category attributes
    // GET /api/admin/categories/{catId}/attributes
    // =========================
    [HttpGet("{catId:int}/attributes")]
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
            x.AttributeId,
            x.IsRequired,
            x.IsFilterable,
            x.SortOrder,
            x.IsVariant, // ✅ AJOUT

            attribute = new
            {
                x.Attribute.Id,
                x.Attribute.Code,
                x.Attribute.Name,
                x.Attribute.DataType,
                x.Attribute.IsVariant,

                options = (x.Attribute.Options ?? new List<ProductAttributeOption>())
             .OrderBy(o => o.SortOrder)
             .ThenBy(o => o.Value)
             .Select(o => new { o.Id, o.Value, o.SortOrder })
             .ToList()
            }
        }).ToList();

        return Ok(outRows);
    }

    // =========================
    // POST replace category attributes
    // POST /api/admin/categories/{catId}/attributes
    // =========================
    public class SaveCategoryAttributesRequest
    {
        public List<SaveCategoryAttributeItem> Items { get; set; } = new();
    }

    public class SaveCategoryAttributeItem
    {
        public int AttributeId { get; set; }
        public bool IsRequired { get; set; }
        public bool IsFilterable { get; set; }
        public int SortOrder { get; set; }
        public bool IsVariant { get; set; }
    }

    [HttpPost("{catId:int}/attributes")]
    public async Task<IActionResult> ReplaceCategoryAttributes(int catId, [FromBody] SaveCategoryAttributesRequest req)
    {
        var catExists = await _db.Categories.AnyAsync(c => c.Id == catId);
        if (!catExists)
            return NotFound(new { ok = false, message = "Catégorie introuvable." });

        // supprime tout
        var olds = await _db.CategoryAttributes.Where(x => x.CategoryId == catId).ToListAsync();
        _db.CategoryAttributes.RemoveRange(olds);

        // ✅ on charge les flags IsVariant depuis dbo.Attributes (source de vérité)
        var attrIds = (req?.Items ?? new()).Select(x => x.AttributeId).Where(id => id > 0).Distinct().ToList();

        var isVariantMap = await _db.ProductAttributes
            .Where(a => attrIds.Contains(a.Id))
            .Select(a => new { a.Id, a.IsVariant })
            .ToDictionaryAsync(x => x.Id, x => x.IsVariant);

        foreach (var it in (req?.Items ?? new()))
        {
            if (it.AttributeId <= 0) continue;

            _db.CategoryAttributes.Add(new CategoryAttribute
            {
                CategoryId = catId,
                AttributeId = it.AttributeId,
                IsRequired = it.IsRequired,
                IsFilterable = it.IsFilterable,
                SortOrder = it.SortOrder,

                // ✅ IMPORTANT : on prend la valeur depuis dbo.Attributes
                IsVariant = isVariantMap.TryGetValue(it.AttributeId, out var v) && v
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { ok = true, count = req?.Items?.Count ?? 0 });
    }


}
