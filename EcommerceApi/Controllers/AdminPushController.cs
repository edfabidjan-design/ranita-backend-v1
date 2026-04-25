using EcommerceApi.Data;
using EcommerceApi.Models;
using EcommerceApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceApi.Controllers;

public class RegisterAdminTokenDto
{
    public string Token { get; set; } = "";
    public string? Platform { get; set; }
    public string? DeviceName { get; set; }
}

[ApiController]
[Route("api/admin/push")]
[Authorize(Roles = "SUPER_ADMIN,ADMIN,MANAGER")]
public class AdminPushController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminPushController(AppDbContext db) => _db = db;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterAdminTokenDto dto)
    {
        var token = (dto.Token ?? "").Trim();
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { ok = false, message = "Token vide." });

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var existing = await _db.AdminDeviceTokens.FirstOrDefaultAsync(x => x.Token == token);

        if (existing == null)
        {
            var platform = (dto.Platform ?? "web").Trim().ToLowerInvariant();
            var device = (dto.DeviceName ?? "").Trim();

            // ✅ Désactive anciens tokens du même device (évite doublons)
            if (!string.IsNullOrWhiteSpace(device))
            {
                var olds = await _db.AdminDeviceTokens
                    .Where(x => x.UserId == userId
                             && x.IsActive
                             && x.Platform == platform
                             && x.DeviceName == device
                             && x.Token != token)
                    .ToListAsync();

                foreach (var o in olds)
                {
                    o.IsActive = false;
                    o.UpdatedAt = DateTime.UtcNow;
                }
            }

            _db.AdminDeviceTokens.Add(new AdminDeviceToken
            {
                UserId = userId,
                Token = token,
                Platform = platform,
                DeviceName = string.IsNullOrWhiteSpace(device) ? null : device,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.UserId = userId;
            existing.IsActive = true;

            if (!string.IsNullOrWhiteSpace(dto.Platform))
                existing.Platform = dto.Platform.Trim().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(dto.DeviceName))
                existing.DeviceName = dto.DeviceName.Trim();

            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

  

}