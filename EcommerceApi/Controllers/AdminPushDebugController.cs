using EcommerceApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/admin/push")]
[Authorize(Roles = "SUPER_ADMIN,ADMIN,MANAGER")]
public class AdminPushDebugController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminPushDebugController(AppDbContext db) => _db = db;

    [HttpGet("mytokens")]
    public async Task<IActionResult> MyTokens()
    {
        // ⚠️ adapte selon comment tu stockes l'userId dans le token JWT
        // si tu as ClaimTypes.NameIdentifier => récupère-le
        var uidClaim = User.Claims.FirstOrDefault(c =>
            c.Type.EndsWith("/nameidentifier") || c.Type == "sub" || c.Type == "id");

        if (uidClaim == null) return Ok(new { ok = false, message = "UserId claim introuvable" });
        if (!int.TryParse(uidClaim.Value, out var userId)) return Ok(new { ok = false, message = "UserId invalide" });

        var list = await _db.AdminDeviceTokens
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new { x.Id, x.Platform, x.DeviceName, x.IsActive, x.CreatedAt, x.UpdatedAt })
            .ToListAsync();

        return Ok(new { ok = true, userId, count = list.Count, items = list });
    }
}