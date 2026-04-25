using EcommerceApi.Data;
using EcommerceApi.Dtos;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/admin/customer-reviews")]
[Authorize(Roles = "SuperAdmin,Admin,Manager")]
public class AdminCustomerReviewsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminCustomerReviewsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.CustomerReviews
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveCustomerReviewDto dto)
    {
        var item = new CustomerReview
        {
            CustomerName = dto.CustomerName,
            CustomerRole = dto.CustomerRole,
            ReviewText = dto.ReviewText,
            Rating = Math.Clamp(dto.Rating, 1, 5),
            AvatarUrl = dto.AvatarUrl,
            ProductName = dto.ProductName,
            ProductLink = dto.ProductLink,
            City = dto.City,
            IsVerified = dto.IsVerified,
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder
        };

        _db.CustomerReviews.Add(item);
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SaveCustomerReviewDto dto)
    {
        var item = await _db.CustomerReviews.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();

        item.CustomerName = dto.CustomerName;
        item.CustomerRole = dto.CustomerRole;
        item.ReviewText = dto.ReviewText;
        item.Rating = Math.Clamp(dto.Rating, 1, 5);
        item.AvatarUrl = dto.AvatarUrl;
        item.ProductName = dto.ProductName;
        item.ProductLink = dto.ProductLink;
        item.City = dto.City;
        item.IsVerified = dto.IsVerified;
        item.IsActive = dto.IsActive;
        item.DisplayOrder = dto.DisplayOrder;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.CustomerReviews.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();

        _db.CustomerReviews.Remove(item);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Supprimé" });
    }
}