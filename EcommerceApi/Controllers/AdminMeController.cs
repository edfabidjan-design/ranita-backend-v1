using EcommerceApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/admin/me")]
[Authorize]
public class AdminMeController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminMeController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> GetMyPermissions()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Utilisateur invalide." });

        var user = await _db.Users
            .Include(u => u.RoleRef)!
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        if (user == null)
            return Unauthorized(new { message = "Utilisateur introuvable ou inactif." });

        var permissions = user.RoleRef?.RolePermissions
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .OrderBy(x => x)
            .ToList() ?? new List<string>();

        return Ok(new
        {
            userId = user.Id,
            username = user.Username,
            role = user.RoleRef == null ? null : new
            {
                id = user.RoleRef.Id,
                name = user.RoleRef.Name,
                code = user.RoleRef.Code
            },
            permissions
        });
    }
}