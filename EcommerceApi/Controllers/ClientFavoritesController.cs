using EcommerceApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/client/favorites")]
[Authorize(Roles = "Client")]
public class ClientFavoritesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ClientFavoritesController(AppDbContext db)
    {
        _db = db;
    }

    private int GetCustomerId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idStr, out var cid))
            throw new Exception("CustomerId introuvable dans le token.");
        return cid;
    }



    // GET /api/client/favorites
    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        var cid = GetCustomerId();


        var favs = await _db.Favorites
            .AsNoTracking()
            .Where(f => f.CustomerId == cid)
            .Include(f => f.Product) // ✅ FIX CRITIQUE
            .OrderByDescending(f => f.Id)
            .Select(f => new
            {
                productId = f.ProductId,
                createdAtUtc = f.CreatedAtUtc,
                product = new
                {
                    id = f.Product.Id,
                    name = f.Product.Name,
                    price = f.Product.Price,
                    pricePromo = f.Product.PricePromo,
                    isActive = f.Product.IsActive
                }
            })
            .ToListAsync();

        var productIds = favs.Select(x => x.productId).Distinct().ToList();

        var imagesMap = await _db.ProductImages
            .AsNoTracking()
            .Where(pi => productIds.Contains(pi.ProductId))
            .OrderByDescending(pi => pi.IsMain)
            .ThenByDescending(pi => pi.Id)
            .GroupBy(pi => pi.ProductId)
            .Select(g => new { ProductId = g.Key, Url = g.First().Url })
            .ToDictionaryAsync(x => x.ProductId, x => x.Url);

        var result = favs.Select(x => new
        {
            x.productId,
            x.createdAtUtc,
            product = new
            {
                x.product.id,
                x.product.name,
                x.product.price,
                x.product.pricePromo,
                x.product.isActive,
                imageUrl = imagesMap.TryGetValue(x.productId, out var url) ? url : null
            }
        });

        return Ok(new { ok = true, items = result });
    }

    // GET /api/client/favorites/ids
    [HttpGet("ids")]
    public async Task<IActionResult> GetIds()
    {
        var cid = GetCustomerId();

        var ids = await _db.Favorites
            .AsNoTracking()
            .Where(f => f.CustomerId == cid)
            .Select(f => f.ProductId)
            .ToListAsync();

        return Ok(new { ok = true, items = ids });
    }

    // POST /api/client/favorites/{productId}
    [HttpPost("{productId:int}")]
    public async Task<IActionResult> Add(int productId)
    {
        var cid = GetCustomerId();


        // sécurité: produit existe
        var exists = await _db.Products.AnyAsync(p => p.Id == productId);
        if (!exists) return NotFound(new { ok = false, message = "Produit introuvable." });

        // évite doublon
        var already = await _db.Favorites.AnyAsync(f => f.CustomerId == cid && f.ProductId == productId);
        if (already) return Ok(new { ok = true, message = "Déjà en favoris." });

        _db.Favorites.Add(new EcommerceApi.Models.Favorite
        {
            CustomerId = cid,
            ProductId = productId,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return Ok(new { ok = true, message = "Ajouté aux favoris." });
    }

    // DELETE /api/client/favorites/{productId}
    // DELETE /api/client/favorites/{productId}
    [HttpDelete("{productId:int}")]
    public async Task<IActionResult> Remove(int productId)
    {
        var cid = GetCustomerId();

        var fav = await _db.Favorites
            .FirstOrDefaultAsync(f => f.CustomerId == cid && f.ProductId == productId);

        if (fav == null)
            return Ok(new { ok = true, message = "Déjà retiré." });

        _db.Favorites.Remove(fav);
        await _db.SaveChangesAsync();

        return Ok(new { ok = true, message = "Retiré des favoris." });
    }

}
