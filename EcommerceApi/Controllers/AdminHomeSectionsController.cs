using EcommerceApi.Data;
using EcommerceApi.Dtos;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/admin/home-sections")]
[Authorize(Roles = "SuperAdmin,Admin,Manager")]
public class AdminHomeSectionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminHomeSectionsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var sections = await _db.HomeSections
            .Include(x => x.Items.OrderBy(i => i.DisplayOrder))
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();

        return Ok(sections);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveHomeSectionDto dto)
    {
        var exists = await _db.HomeSections.AnyAsync(x => x.SectionKey == dto.SectionKey);
        if (exists) return BadRequest(new { message = "SectionKey déjà utilisé." });

        var item = new HomeSection
        {
            SectionKey = dto.SectionKey,
            Title = dto.Title,
            SubTitle = dto.SubTitle,
            Description = dto.Description,
            PrimaryButtonText = dto.PrimaryButtonText,
            PrimaryButtonLink = dto.PrimaryButtonLink,
            SecondaryButtonText = dto.SecondaryButtonText,
            SecondaryButtonLink = dto.SecondaryButtonLink,
            ImageUrl = dto.ImageUrl,
            BadgeText = dto.BadgeText,
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder,
            BackgroundColor = dto.BackgroundColor,
            TextColor = dto.TextColor
        };

        _db.HomeSections.Add(item);
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SaveHomeSectionDto dto)
    {
        var item = await _db.HomeSections.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();

        var exists = await _db.HomeSections.AnyAsync(x => x.SectionKey == dto.SectionKey && x.Id != id);
        if (exists) return BadRequest(new { message = "SectionKey déjà utilisé." });

        item.SectionKey = dto.SectionKey;
        item.Title = dto.Title;
        item.SubTitle = dto.SubTitle;
        item.Description = dto.Description;
        item.PrimaryButtonText = dto.PrimaryButtonText;
        item.PrimaryButtonLink = dto.PrimaryButtonLink;
        item.SecondaryButtonText = dto.SecondaryButtonText;
        item.SecondaryButtonLink = dto.SecondaryButtonLink;
        item.ImageUrl = dto.ImageUrl;
        item.BadgeText = dto.BadgeText;
        item.IsActive = dto.IsActive;
        item.DisplayOrder = dto.DisplayOrder;
        item.BackgroundColor = dto.BackgroundColor;
        item.TextColor = dto.TextColor;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.HomeSections.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();

        _db.HomeSections.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Supprimé" });
    }
}