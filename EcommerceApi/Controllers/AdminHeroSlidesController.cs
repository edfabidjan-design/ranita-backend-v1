using EcommerceApi.Data;
using EcommerceApi.Dtos;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/admin/hero-slides")]
[Authorize]
public class AdminHeroSlidesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public AdminHeroSlidesController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.HeroSlides
            .AsNoTracking()
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

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] EcommerceApi.Dtos.HeroSlideCreateDto dto)
    {
        string? imageUrl = null;

        if (dto.Image != null && dto.Image.Length > 0)
        {
            imageUrl = await SaveImageAsync(dto.Image);
        }

        var entity = new HeroSlide
        {
            Title = dto.Title?.Trim() ?? "",
            Subtitle = dto.Subtitle?.Trim(),
            BadgeText = dto.BadgeText?.Trim(),
            SmallTag = dto.SmallTag?.Trim(),
            PrimaryButtonText = dto.PrimaryButtonText?.Trim(),
            PrimaryButtonUrl = dto.PrimaryButtonUrl?.Trim(),
            SecondaryButtonText = dto.SecondaryButtonText?.Trim(),
            SecondaryButtonUrl = dto.SecondaryButtonUrl?.Trim(),
            ImageUrl = imageUrl,
            Theme = string.IsNullOrWhiteSpace(dto.Theme) ? "brand" : dto.Theme.Trim(),
            AccentColor = string.IsNullOrWhiteSpace(dto.AccentColor) ? "#22c55e" : dto.AccentColor.Trim(),
            HighlightText = dto.HighlightText?.Trim(),
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,

            RightBadgeText = dto.RightBadgeText?.Trim(),
            RightTitle = dto.RightTitle?.Trim(),
            RightSubtitle = dto.RightSubtitle?.Trim(),
            RightButtonText = dto.RightButtonText?.Trim(),
            RightButtonUrl = dto.RightButtonUrl?.Trim(),

            StatsTitle = dto.StatsTitle?.Trim(),
            Stat1Value = dto.Stat1Value?.Trim(),
            Stat1Label = dto.Stat1Label?.Trim(),
            Stat2Value = dto.Stat2Value?.Trim(),
            Stat2Label = dto.Stat2Label?.Trim(),
            Stat3Value = dto.Stat3Value?.Trim(),
            Stat3Label = dto.Stat3Label?.Trim(),
        };

        _db.HeroSlides.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(new HeroSlideDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Subtitle = entity.Subtitle,
            BadgeText = entity.BadgeText,
            SmallTag = entity.SmallTag,
            PrimaryButtonText = entity.PrimaryButtonText,
            PrimaryButtonUrl = entity.PrimaryButtonUrl,
            SecondaryButtonText = entity.SecondaryButtonText,
            SecondaryButtonUrl = entity.SecondaryButtonUrl,
            ImageUrl = entity.ImageUrl,
            Theme = entity.Theme,
            AccentColor = entity.AccentColor,
            HighlightText = entity.HighlightText,
            DisplayOrder = entity.DisplayOrder,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,

            RightBadgeText = dto.RightBadgeText?.Trim(),
            RightTitle = dto.RightTitle?.Trim(),
            RightSubtitle = dto.RightSubtitle?.Trim(),
            RightButtonText = dto.RightButtonText?.Trim(),
            RightButtonUrl = dto.RightButtonUrl?.Trim(),

            StatsTitle = dto.StatsTitle?.Trim(),
            Stat1Value = dto.Stat1Value?.Trim(),
            Stat1Label = dto.Stat1Label?.Trim(),
            Stat2Value = dto.Stat2Value?.Trim(),
            Stat2Label = dto.Stat2Label?.Trim(),
            Stat3Value = dto.Stat3Value?.Trim(),
            Stat3Label = dto.Stat3Label?.Trim(),
        });
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(int id, [FromForm] EcommerceApi.Dtos.HeroSlideUpdateDto dto)
    {
        var entity = await _db.HeroSlides.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null)
            return NotFound(new { message = "Slide introuvable." });

        entity.Title = dto.Title?.Trim() ?? "";
        entity.Subtitle = dto.Subtitle?.Trim();
        entity.BadgeText = dto.BadgeText?.Trim();
        entity.SmallTag = dto.SmallTag?.Trim();
        entity.PrimaryButtonText = dto.PrimaryButtonText?.Trim();
        entity.PrimaryButtonUrl = dto.PrimaryButtonUrl?.Trim();
        entity.SecondaryButtonText = dto.SecondaryButtonText?.Trim();
        entity.SecondaryButtonUrl = dto.SecondaryButtonUrl?.Trim();
        entity.Theme = string.IsNullOrWhiteSpace(dto.Theme) ? "brand" : dto.Theme.Trim();
        entity.AccentColor = string.IsNullOrWhiteSpace(dto.AccentColor) ? "#22c55e" : dto.AccentColor.Trim();
        entity.HighlightText = dto.HighlightText?.Trim();
        entity.DisplayOrder = dto.DisplayOrder;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        entity.RightBadgeText = dto.RightBadgeText?.Trim();
        entity.RightTitle = dto.RightTitle?.Trim();
        entity.RightSubtitle = dto.RightSubtitle?.Trim();
        entity.RightButtonText = dto.RightButtonText?.Trim();
        entity.RightButtonUrl = dto.RightButtonUrl?.Trim();

        entity.StatsTitle = dto.StatsTitle?.Trim();
        entity.Stat1Value = dto.Stat1Value?.Trim();
        entity.Stat1Label = dto.Stat1Label?.Trim();
        entity.Stat2Value = dto.Stat2Value?.Trim();
        entity.Stat2Label = dto.Stat2Label?.Trim();
        entity.Stat3Value = dto.Stat3Value?.Trim();
        entity.Stat3Label = dto.Stat3Label?.Trim();

        if (dto.Image != null && dto.Image.Length > 0)
        {
            DeletePhysicalFile(entity.ImageUrl);
            entity.ImageUrl = await SaveImageAsync(dto.Image);
        }

        await _db.SaveChangesAsync();

        return Ok(new { message = "Slide modifié avec succès." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.HeroSlides.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null)
            return NotFound(new { message = "Slide introuvable." });

        DeletePhysicalFile(entity.ImageUrl);

        _db.HeroSlides.Remove(entity);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Slide supprimé avec succès." });
    }

    [HttpPut("{id:int}/toggle")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var slide = await _db.HeroSlides.FindAsync(id);
        if (slide == null)
            return NotFound(new { message = "Slide introuvable." });

        slide.IsActive = !slide.IsActive;
        slide.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { slide.Id, slide.IsActive });
    }

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };

        if (!allowed.Contains(ext))
            throw new Exception("Format image non autorisé. Utilisez jpg, jpeg, png ou webp.");

        var folder = Path.Combine(_env.WebRootPath, "uploads", "hero-slides");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var fileName = $"hero_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folder, fileName);

        using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/hero-slides/{fileName}";
    }

    private void DeletePhysicalFile(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return;

        var relativePath = imageUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
        var fullPath = Path.Combine(_env.WebRootPath, relativePath);

        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);
    }
}