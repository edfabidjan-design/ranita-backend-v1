using EcommerceApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/vendor")]
[Authorize(Roles = "Vendor")]
public class VendorMeController : ControllerBase
{
    private readonly AppDbContext _db;
    public VendorMeController(AppDbContext db) => _db = db;

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var vendorIdStr =
            User.FindFirstValue("vendorId") ??
            User.FindFirstValue("VendorId");

        if (!int.TryParse(vendorIdStr, out var vendorId))
            return Unauthorized(new { ok = false, message = "VendorId manquant." });

        var vendor = await _db.Vendors.AsNoTracking()
            .Where(v => v.Id == vendorId)
            .Select(v => new
            {
                v.Id,
                v.Name,
                v.Slug,
                v.Email,
                v.Phone,
                v.Status,
                v.CommissionRate,
                v.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (vendor == null)
            return Unauthorized(new { ok = false, message = "Boutique introuvable." });

        return Ok(new
        {
            ok = true,
            storeName = vendor.Name,   // ✅ simple pour le JS
            vendor
        });
    }
}