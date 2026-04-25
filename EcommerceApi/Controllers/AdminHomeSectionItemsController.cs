using EcommerceApi.Data;
using EcommerceApi.Dtos;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/admin/home-section-items")]
[Authorize(Roles = "SuperAdmin,Admin,Manager")]
public class AdminHomeSectionItemsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminHomeSectionItemsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveHomeSectionItemDto dto)
    {
        var section = await _db.HomeSections.FirstOrDefaultAsync(x => x.Id == dto.HomeSectionId);
        if (section == null) return BadRequest(new { message = "Section introuvable." });

        var item = new HomeSectionItem
        {
            HomeSectionId = dto.HomeSectionId,
            ItemType = dto.ItemType,
            Title = dto.Title,
            SubTitle = dto.SubTitle,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            IconClass = dto.IconClass,
            ButtonText = dto.ButtonText,
            ButtonLink = dto.ButtonLink,
            BadgeText = dto.BadgeText,
            PriceText = dto.PriceText,
            MetaText = dto.MetaText,
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder
        };

        _db.HomeSectionItems.Add(item);
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SaveHomeSectionItemDto dto)
    {
        var item = await _db.HomeSectionItems.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();

        item.HomeSectionId = dto.HomeSectionId;
        item.ItemType = dto.ItemType;
        item.Title = dto.Title;
        item.SubTitle = dto.SubTitle;
        item.Description = dto.Description;
        item.ImageUrl = dto.ImageUrl;
        item.IconClass = dto.IconClass;
        item.ButtonText = dto.ButtonText;
        item.ButtonLink = dto.ButtonLink;
        item.BadgeText = dto.BadgeText;
        item.PriceText = dto.PriceText;
        item.MetaText = dto.MetaText;
        item.IsActive = dto.IsActive;
        item.DisplayOrder = dto.DisplayOrder;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.HomeSectionItems.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();

        _db.HomeSectionItems.Remove(item);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Supprimé" });
    }


}