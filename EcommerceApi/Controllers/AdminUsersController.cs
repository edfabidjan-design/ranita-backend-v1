using EcommerceApi.Data;
using EcommerceApi.Dtos;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "SUPER_ADMIN,ADMIN,MANAGER")]
public class AdminUsersController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminUsersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.Users
            .AsNoTracking()
            .Include(x => x.RoleRef)
            .OrderByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Username,
                x.RoleId,
                role = x.RoleRef != null ? x.RoleRef.Name : x.Role,
                roleCode = x.RoleRef != null ? x.RoleRef.Code : x.Role,
                x.IsActive,
                x.CreatedAt
            })
            .ToListAsync();

        return Ok(new { ok = true, items });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { ok = false, message = "Username et Password obligatoires." });

        if (!dto.RoleId.HasValue || dto.RoleId.Value <= 0)
            return BadRequest(new { ok = false, message = "Rôle obligatoire." });

        var username = dto.Username.Trim();

        var exists = await _db.Users.AnyAsync(x => x.Username == username);
        if (exists)
            return BadRequest(new { ok = false, message = "Username déjà utilisé." });

        var role = await _db.Roles.FirstOrDefaultAsync(x => x.Id == dto.RoleId.Value);
        if (role == null)
            return BadRequest(new { ok = false, message = "Rôle invalide." });

        var legacyRole = role.Code switch
        {
            "SUPER_ADMIN" => "SuperAdmin",
            "ADMIN" => "Admin",
            "MANAGER" => "Manager",
            _ => role.Name
        };

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId = role.Id,
            Role = legacyRole, // compatibilité temporaire
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            ok = true,
            item = new
            {
                user.Id,
                user.Username,
                user.RoleId,
                role = role.Name,
                roleCode = role.Code,
                user.IsActive
            }
        });
    }

    [HttpPatch("{id:int}/active")]
    public async Task<IActionResult> SetActive(int id, [FromQuery] bool active)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { ok = false, message = "User introuvable." });

        user.IsActive = active;
        await _db.SaveChangesAsync();

        return Ok(new { ok = true });
    }

    [HttpPatch("{id:int}/role")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateUserRoleDto dto)
    {
        if (dto == null || !dto.RoleId.HasValue || dto.RoleId.Value <= 0)
            return BadRequest(new { ok = false, message = "Rôle obligatoire." });

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null)
            return NotFound(new { ok = false, message = "Utilisateur introuvable." });

        var role = await _db.Roles.FirstOrDefaultAsync(x => x.Id == dto.RoleId.Value);
        if (role == null)
            return BadRequest(new { ok = false, message = "Rôle invalide." });

        var legacyRole = role.Code switch
        {
            "SUPER_ADMIN" => "SuperAdmin",
            "ADMIN" => "Admin",
            "MANAGER" => "Manager",
            _ => role.Name
        };

        user.RoleId = role.Id;
        user.Role = legacyRole;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            ok = true,
            item = new
            {
                user.Id,
                user.Username,
                user.RoleId,
                role = role.Name,
                roleCode = role.Code,
                user.IsActive
            }
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateUserDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Username))
            return BadRequest(new { ok = false, message = "Username obligatoire." });

        if (!dto.RoleId.HasValue || dto.RoleId.Value <= 0)
            return BadRequest(new { ok = false, message = "Rôle obligatoire." });

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null)
            return NotFound(new { ok = false, message = "Utilisateur introuvable." });

        var username = dto.Username.Trim();

        var exists = await _db.Users.AnyAsync(x => x.Username == username && x.Id != id);
        if (exists)
            return BadRequest(new { ok = false, message = "Username déjà utilisé." });

        var role = await _db.Roles.FirstOrDefaultAsync(x => x.Id == dto.RoleId.Value);
        if (role == null)
            return BadRequest(new { ok = false, message = "Rôle invalide." });

        var legacyRole = role.Code switch
        {
            "SUPER_ADMIN" => "SuperAdmin",
            "ADMIN" => "Admin",
            "MANAGER" => "Manager",
            _ => role.Name
        };

        user.Username = username;
        user.RoleId = role.Id;
        user.Role = legacyRole;

        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        await _db.SaveChangesAsync();

        return Ok(new { ok = true });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null)
            return NotFound(new { ok = false, message = "Utilisateur introuvable." });

        // sécurité : éviter de supprimer son propre compte
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(currentUserId, out var me) && me == id)
            return BadRequest(new { ok = false, message = "Vous ne pouvez pas supprimer votre propre compte." });

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return Ok(new { ok = true, message = "Utilisateur supprimé." });
    }
}