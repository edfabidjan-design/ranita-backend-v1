using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers
{
    [ApiController]
    [Route("api/admin/flash-deals")]
    [Authorize]
    public class AdminFlashDealsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminFlashDealsController(AppDbContext db)
        {
            _db = db;
        }

        public class FlashDealSaveDto
        {
            public int ProductId { get; set; }
            public decimal DiscountPercent { get; set; }
            public DateTime StartAt { get; set; }
            public DateTime EndAt { get; set; }
            public bool IsActive { get; set; } = true;
            public int DisplayOrder { get; set; } = 1;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _db.FlashDeals
                .Include(x => x.Product)
                .ThenInclude(p => p!.Images)
                .OrderBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.ProductId,
                    ProductName = x.Product != null ? x.Product.Name : "",
                    ProductPrice = x.Product != null ? x.Product.Price : 0,
                    ProductPricePromo = x.Product != null ? x.Product.PricePromo : null,
                    ProductStock = x.Product != null ? x.Product.Stock : 0,
                    ProductImageUrl = x.Product != null
                        ? x.Product.Images
                            .OrderBy(i => i.SortOrder)
                            .Select(i => i.Url)
                            .FirstOrDefault()
                        : null,
                    x.DiscountPercent,
                    x.StartAt,
                    x.EndAt,
                    x.IsActive,
                    x.DisplayOrder,
                    x.CreatedAt
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetEligibleProducts()
        {
            var products = await _db.Products
                .Include(p => p.Images)
                .Where(p => p.IsActive && !p.IsDeleted && p.PublishedStatus == "Published")
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.PricePromo,
                    p.Stock,
                    ImageUrl = p.Images
                        .OrderBy(i => i.SortOrder)
                        .Select(i => i.Url)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(products);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FlashDealSaveDto dto)
        {
            if (dto.ProductId <= 0)
                return BadRequest("Le produit est obligatoire.");

            if (dto.DiscountPercent <= 0 || dto.DiscountPercent >= 100)
                return BadRequest("La réduction doit être comprise entre 1 et 99.");

            if (dto.EndAt <= dto.StartAt)
                return BadRequest("La date de fin doit être supérieure à la date de début.");

            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId);

            if (product == null)
                return NotFound("Produit introuvable.");

            if (!product.IsActive || product.IsDeleted || product.PublishedStatus != "Published")
                return BadRequest("Le produit sélectionné n'est pas éligible aux Flash Deals.");

            var entity = new FlashDeal
            {
                ProductId = dto.ProductId,
                DiscountPercent = dto.DiscountPercent,
                StartAt = dto.StartAt,
                EndAt = dto.EndAt,
                IsActive = dto.IsActive,
                DisplayOrder = dto.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            };

            _db.FlashDeals.Add(entity);
            await _db.SaveChangesAsync();

            return Ok(entity);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] FlashDealSaveDto dto)
        {
            var entity = await _db.FlashDeals.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                return NotFound("Flash Deal introuvable.");

            if (dto.ProductId <= 0)
                return BadRequest("Le produit est obligatoire.");

            if (dto.DiscountPercent <= 0 || dto.DiscountPercent >= 100)
                return BadRequest("La réduction doit être comprise entre 1 et 99.");

            if (dto.EndAt <= dto.StartAt)
                return BadRequest("La date de fin doit être supérieure à la date de début.");

            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId);

            if (product == null)
                return NotFound("Produit introuvable.");

            if (!product.IsActive || product.IsDeleted || product.PublishedStatus != "Published")
                return BadRequest("Le produit sélectionné n'est pas éligible aux Flash Deals.");

            entity.ProductId = dto.ProductId;
            entity.DiscountPercent = dto.DiscountPercent;
            entity.StartAt = dto.StartAt;
            entity.EndAt = dto.EndAt;
            entity.IsActive = dto.IsActive;
            entity.DisplayOrder = dto.DisplayOrder;

            await _db.SaveChangesAsync();

            return Ok(entity);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.FlashDeals.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                return NotFound("Flash Deal introuvable.");

            _db.FlashDeals.Remove(entity);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Flash Deal supprimé." });
        }
    }
}