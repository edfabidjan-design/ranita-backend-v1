using EcommerceApi.Data;
using EcommerceApi.Dtos;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/admin/promo-banners")]
[Authorize(Roles = "SuperAdmin,Admin,Manager")]
public class AdminPromoBannersController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminPromoBannersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.PromoBanners
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Priority)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SavePromoBannerDto dto)
    {
        if (dto.StartAt.HasValue && dto.EndAt.HasValue && dto.StartAt > dto.EndAt)
            return BadRequest("La date de début doit être inférieure à la date de fin.");

        var item = new PromoBanner
        {
            Title = dto.Title?.Trim() ?? "",
            SubTitle = dto.SubTitle?.Trim(),
            Description = dto.Description?.Trim(),
            PromoCode = dto.PromoCode?.Trim() ?? "",

            PrimaryButtonText = dto.PrimaryButtonText?.Trim(),
            PrimaryButtonLink = dto.PrimaryButtonLink?.Trim(),

            SecondaryButtonText = dto.SecondaryButtonText?.Trim(),
            SecondaryButtonLink = dto.SecondaryButtonLink?.Trim(),

            ImageUrl = dto.ImageUrl?.Trim(),
            MobileImageUrl = dto.MobileImageUrl?.Trim(),

            BackgroundColor = dto.BackgroundColor?.Trim(),
            TextColor = dto.TextColor?.Trim(),

            SideTitle = dto.SideTitle?.Trim() ?? "",
            SideText = dto.SideText?.Trim() ?? "",

            StartAt = dto.StartAt,
            EndAt = dto.EndAt,

            BackgroundImageUrl = dto.BackgroundImageUrl?.Trim() ?? "",
            Theme = string.IsNullOrWhiteSpace(dto.Theme) ? "promo" : dto.Theme.Trim().ToLowerInvariant(),
            AccentColor = string.IsNullOrWhiteSpace(dto.AccentColor) ? "#22c55e" : dto.AccentColor.Trim(),
            Position = string.IsNullOrWhiteSpace(dto.Position) ? "after-hero" : dto.Position.Trim().ToLowerInvariant(),
            Priority = dto.Priority <= 0 ? 1 : dto.Priority,

            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = DateTime.UtcNow
        };

        _db.PromoBanners.Add(item);
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SavePromoBannerDto dto)
    {
        if (dto.StartAt.HasValue && dto.EndAt.HasValue && dto.StartAt > dto.EndAt)
            return BadRequest("La date de début doit être inférieure à la date de fin.");

        var item = await _db.PromoBanners.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();

        item.Title = dto.Title?.Trim() ?? "";
        item.SubTitle = dto.SubTitle?.Trim();
        item.Description = dto.Description?.Trim();
        item.PromoCode = dto.PromoCode?.Trim() ?? "";

        item.PrimaryButtonText = dto.PrimaryButtonText?.Trim();
        item.PrimaryButtonLink = dto.PrimaryButtonLink?.Trim();

        item.SecondaryButtonText = dto.SecondaryButtonText?.Trim();
        item.SecondaryButtonLink = dto.SecondaryButtonLink?.Trim();

        item.ImageUrl = dto.ImageUrl?.Trim();
        item.MobileImageUrl = dto.MobileImageUrl?.Trim();

        item.BackgroundColor = dto.BackgroundColor?.Trim();
        item.TextColor = dto.TextColor?.Trim();

        item.SideTitle = dto.SideTitle?.Trim() ?? "";
        item.SideText = dto.SideText?.Trim() ?? "";

        item.StartAt = dto.StartAt;
        item.EndAt = dto.EndAt;

        item.BackgroundImageUrl = dto.BackgroundImageUrl?.Trim() ?? "";
        item.Theme = string.IsNullOrWhiteSpace(dto.Theme) ? "promo" : dto.Theme.Trim().ToLowerInvariant();
        item.AccentColor = string.IsNullOrWhiteSpace(dto.AccentColor) ? "#22c55e" : dto.AccentColor.Trim();
        item.Position = string.IsNullOrWhiteSpace(dto.Position) ? "after-hero" : dto.Position.Trim().ToLowerInvariant();
        item.Priority = dto.Priority <= 0 ? 1 : dto.Priority;

        item.IsActive = dto.IsActive;
        item.DisplayOrder = dto.DisplayOrder;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.PromoBanners.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();

        _db.PromoBanners.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Supprimé" });
    }
}