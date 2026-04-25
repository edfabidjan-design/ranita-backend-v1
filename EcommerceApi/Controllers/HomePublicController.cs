using EcommerceApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/home")]
public class HomePublicController : ControllerBase
{
    private readonly AppDbContext _db;

    public HomePublicController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetHomeData()
    {
        var now = DateTime.UtcNow;

        var promoBanner = await _db.PromoBanners
            .Where(x => x.IsActive &&
                        (!x.StartAt.HasValue || x.StartAt <= now) &&
                        (!x.EndAt.HasValue || x.EndAt >= now))
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Priority)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.SubTitle,
                x.Description,
                x.PromoCode,
                x.PrimaryButtonText,
                x.PrimaryButtonLink,
                x.SecondaryButtonText,
                x.SecondaryButtonLink,
                x.ImageUrl,
                x.MobileImageUrl,
                x.BackgroundColor,
                x.TextColor,
                x.SideTitle,
                x.SideText,
                x.BackgroundImageUrl,
                x.Theme,
                x.AccentColor,
                x.Position
            })
            .FirstOrDefaultAsync();

        var reviews = await _db.CustomerReviews
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.CustomerName,
                x.CustomerRole,
                x.ReviewText,
                x.Rating,
                x.AvatarUrl,
                x.ProductName,
                x.ProductLink,
                x.City,
                x.IsVerified
            })
            .ToListAsync();

        var sections = await _db.HomeSections
     .Where(x => x.IsActive)
     .OrderBy(x => x.DisplayOrder)
     .Select(x => new
     {
         x.Id,
         x.SectionKey,
         x.Title,
         x.SubTitle,
         x.Description,
         x.PrimaryButtonText,
         x.PrimaryButtonLink,
         x.SecondaryButtonText,
         x.SecondaryButtonLink,
         x.ImageUrl,
         x.BadgeText,
         x.BackgroundColor,
         x.TextColor,
         x.LayoutType,
         Items = x.Items
             .Where(i => i.IsActive)
             .OrderBy(i => i.DisplayOrder)
             .Select(i => new
             {
                 i.Id,
                 i.ItemType,
                 i.Title,
                 i.SubTitle,
                 i.Description,
                 i.ImageUrl,
                 i.IconClass,
                 i.ButtonText,
                 i.ButtonLink,
                 i.BadgeText,
                 i.PriceText,
                 i.MetaText
             }).ToList()
     })
     .ToListAsync();

        return Ok(new
        {
            promoBanner,
            reviews,
            sections
        });
    }
}