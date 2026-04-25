using EcommerceApi.Data;
using EcommerceApi.Dtos;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/produits/{productId:int}/images")]
public class ProductImagesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProductImagesController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromRoute] int productId)
    {
        var exists = await _db.Products.AnyAsync(p => p.Id == productId);
        if (!exists) return NotFound(new { ok = false, message = "Produit introuvable." });

        var imgs = await _db.ProductImages
            .Where(i => i.ProductId == productId)
            .OrderByDescending(i => i.IsMain)
            .ThenByDescending(i => i.Id)
            .Select(i => new { i.Id, i.Url, i.IsMain })
            .ToListAsync();

        return Ok(new { ok = true, items = imgs });
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Upload(
        [FromRoute] int productId,
        [FromForm] UploadProductImageForm form)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null) return NotFound(new { ok = false, message = "Produit introuvable." });

        if (form.File == null || form.File.Length == 0)
            return BadRequest(new { ok = false, message = "Fichier manquant." });

        var ext = Path.GetExtension(form.File.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowed.Contains(ext))
            return BadRequest(new { ok = false, message = "Format non supporté (jpg, png, webp)." });

        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", "products", productId.ToString());
        Directory.CreateDirectory(folder);

        var safeName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folder, safeName);

        await using (var stream = System.IO.File.Create(fullPath))
            await form.File.CopyToAsync(stream);

        var url = $"/uploads/products/{productId}/{safeName}";

        if (form.IsMain)
        {
            var olds = await _db.ProductImages
                .Where(x => x.ProductId == productId && x.IsMain)
                .ToListAsync();

            foreach (var o in olds) o.IsMain = false;
        }

        var img = new ProductImage
        {
            ProductId = productId,
            FileName = safeName,
            Url = url,
            IsMain = form.IsMain
        };

        _db.ProductImages.Add(img);
        await _db.SaveChangesAsync();

        return Ok(new { ok = true, message = "Image ajoutée.", item = new { img.Id, img.Url, img.IsMain } });
    }

    // ✅ SUPPRIMER une image
    [HttpDelete("{imageId:int}")]
    public async Task<IActionResult> Delete([FromRoute] int productId, [FromRoute] int imageId)
    {
        var img = await _db.ProductImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId);

        if (img == null)
            return NotFound(new { ok = false, message = "Image introuvable." });

        // chemin fichier
        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var fullPath = Path.Combine(webRoot, "uploads", "products", productId.ToString(), img.FileName);

        // suppression base
        _db.ProductImages.Remove(img);
        await _db.SaveChangesAsync();

        // suppression disque (après DB, sans bloquer si fichier manquant)
        try
        {
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }
        catch { /* ignore */ }

        return Ok(new { ok = true, message = "Image supprimée." });
    }


    // ✅ DEFINIR image principale
    [HttpPut("{imageId:int}/main")]
    public async Task<IActionResult> SetMain([FromRoute] int productId, [FromRoute] int imageId)
    {
        var img = await _db.ProductImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId);

        if (img == null)
            return NotFound(new { ok = false, message = "Image introuvable." });

        var olds = await _db.ProductImages
            .Where(x => x.ProductId == productId && x.IsMain)
            .ToListAsync();

        foreach (var o in olds) o.IsMain = false;

        img.IsMain = true;

        await _db.SaveChangesAsync();

        return Ok(new { ok = true, message = "Image principale définie." });
    }



}
