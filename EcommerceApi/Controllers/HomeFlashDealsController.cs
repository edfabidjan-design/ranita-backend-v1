using EcommerceApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers
{
    [ApiController]
    [Route("api/home/flash-deals")]
    public class HomeFlashDealsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public HomeFlashDealsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetFlashDeals()
        {
            var now = DateTime.UtcNow;

            var deals = await _db.FlashDeals
                .Where(x =>
                    x.IsActive &&
                    x.StartAt <= now &&
                    x.EndAt >= now &&
                    x.Product.IsActive &&
                    !x.Product.IsDeleted &&
                    x.Product.PublishedStatus == "Published")
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.ProductId,
                    x.DiscountPercent,
                    x.StartAt,
                    x.EndAt,
                    Product = new
                    {
                        x.Product.Id,
                        x.Product.Name,
                        x.Product.Price,
                        x.Product.PricePromo,
                        x.Product.Stock,
                        ImageUrl = x.Product.Images
                            .OrderBy(i => i.SortOrder)
                            .Select(i => i.Url)
                            .FirstOrDefault()
                    }
                })
                .ToListAsync();

            if (!deals.Any())
            {
                return Ok(new
                {
                    endAt = (DateTime?)null,
                    items = Array.Empty<object>()
                });
            }

            var globalEndAt = deals.Min(x => x.EndAt);

            var items = deals.Select(x =>
            {
                var basePrice = x.Product.PricePromo.HasValue && x.Product.PricePromo.Value > 0
                    ? x.Product.PricePromo.Value
                    : x.Product.Price;

                var discountedPrice = Math.Round(
                    basePrice * (1 - (x.DiscountPercent / 100m)),
                    0,
                    MidpointRounding.AwayFromZero);

                return new
                {
                    x.Id,
                    x.ProductId,
                    x.DiscountPercent,
                    x.StartAt,
                    x.EndAt,
                    product = new
                    {
                        x.Product.Id,
                        x.Product.Name,
                        originalPrice = basePrice,
                        discountedPrice,
                        x.Product.Stock,
                        imageUrl = x.Product.ImageUrl
                    }
                };
            });

            return Ok(new
            {
                endAt = globalEndAt,
                items
            });
        }
    }
}