using EcommerceApi.Data;
using EcommerceApi.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/hero-slides")]
public class HeroSlidesController : ControllerBase
{
    private readonly AppDbContext _db;

    public HeroSlidesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetPublicSlides()
    {
        var items = await _db.HeroSlides
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.Id)
            .Select(x => new HeroSlideDto
            {
                Id = x.Id,
                Title = x.Title,
                Subtitle = x.Subtitle,
                BadgeText = x.BadgeText,
                SmallTag = x.SmallTag,
                PrimaryButtonText = x.PrimaryButtonText,
                PrimaryButtonUrl = x.PrimaryButtonUrl,
                SecondaryButtonText = x.SecondaryButtonText,
                SecondaryButtonUrl = x.SecondaryButtonUrl,
                ImageUrl = x.ImageUrl,
                Theme = x.Theme,
                AccentColor = x.AccentColor,
                HighlightText = x.HighlightText,
                DisplayOrder = x.DisplayOrder,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,

                RightBadgeText = x.RightBadgeText,
                RightTitle = x.RightTitle,
                RightSubtitle = x.RightSubtitle,
                RightButtonText = x.RightButtonText,
                RightButtonUrl = x.RightButtonUrl,

                StatsTitle = x.StatsTitle,
                Stat1Value = x.Stat1Value,
                Stat1Label = x.Stat1Label,
                Stat2Value = x.Stat2Value,
                Stat2Label = x.Stat2Label,
                Stat3Value = x.Stat3Value,
                Stat3Label = x.Stat3Label,
            })
            .ToListAsync();

        return Ok(items);
    }


}