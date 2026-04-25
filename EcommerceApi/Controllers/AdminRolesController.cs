using EcommerceApi.Data;
using EcommerceApi.Dtos;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "SUPER_ADMIN,ADMIN,MANAGER")]
public class AdminRolesController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminRolesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var items = await _db.Roles
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code,
                x.Description,
                x.IsSystemRole
            })
            .ToListAsync();

        return Ok(new { items });
    }

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] RoleCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Nom du rôle obligatoire." });

        if (string.IsNullOrWhiteSpace(dto.Code))
            return BadRequest(new { message = "Code du rôle obligatoire." });

        var code = dto.Code.Trim().ToUpperInvariant();
        var name = dto.Name.Trim();

        var exists = await _db.Roles.AnyAsync(x => x.Code == code || x.Name == name);
        if (exists)
            return BadRequest(new { message = "Ce rôle existe déjà." });

        var role = new Role
        {
            Name = name,
            Code = code,
            Description = dto.Description?.Trim(),
            IsSystemRole = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        return Ok(new { ok = true, roleId = role.Id });
    }

    [HttpPut("roles/{id:int}")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] RoleUpdateDto dto)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(x => x.Id == id);
        if (role == null)
            return NotFound(new { message = "Rôle introuvable." });

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Nom du rôle obligatoire." });

        var name = dto.Name.Trim();

        var exists = await _db.Roles.AnyAsync(x => x.Id != id && x.Name == name);
        if (exists)
            return BadRequest(new { message = "Un rôle avec ce nom existe déjà." });

        role.Name = name;
        role.Description = dto.Description?.Trim();

        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpDelete("roles/{id:int}")]
    public async Task<IActionResult> DeleteRole(int id)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(x => x.Id == id);
        if (role == null)
            return NotFound(new { message = "Rôle introuvable." });

        if (role.IsSystemRole)
            return BadRequest(new { message = "Impossible de supprimer un rôle système." });

        var hasUsers = await _db.Users.AnyAsync(x => x.RoleId == id);
        if (hasUsers)
            return BadRequest(new { message = "Ce rôle est encore attribué à des utilisateurs." });

        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();

        return Ok(new { ok = true });
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions()
    {
        var items = await _db.Permissions
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.Category,
                x.Description
            })
            .ToListAsync();

        return Ok(new { items });
    }

    [HttpGet("roles/{id:int}/permissions")]
    public async Task<IActionResult> GetRolePermissions(int id)
    {
        var role = await _db.Roles
            .Include(x => x.RolePermissions)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (role == null)
            return NotFound(new { message = "Rôle introuvable." });

        var permissionIds = role.RolePermissions
            .Select(x => x.PermissionId)
            .Distinct()
            .ToList();

        return Ok(new
        {
            role = new
            {
                role.Id,
                role.Name,
                role.Code,
                role.Description,
                role.IsSystemRole
            },
            permissionIds
        });
    }

    [HttpPut("roles/{id:int}/permissions")]
    public async Task<IActionResult> UpdateRolePermissions(int id, [FromBody] UpdateRolePermissionsDto dto)
    {
        var roleExists = await _db.Roles.AnyAsync(x => x.Id == id);
        if (!roleExists)
            return NotFound(new { message = "Rôle introuvable." });

        var permissionIds = (dto.PermissionIds ?? new List<int>())
            .Distinct()
            .ToList();

        var validIds = await _db.Permissions
            .Where(x => permissionIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync();

        var existing = await _db.RolePermissions
            .Where(x => x.RoleId == id)
            .ToListAsync();

        if (existing.Count > 0)
        {
            _db.RolePermissions.RemoveRange(existing);
            await _db.SaveChangesAsync();
        }

        if (validIds.Count > 0)
        {
            var newLinks = validIds.Select(pid => new RolePermission
            {
                RoleId = id,
                PermissionId = pid
            }).ToList();

            _db.RolePermissions.AddRange(newLinks);
            await _db.SaveChangesAsync();
        }

        return Ok(new { ok = true });
    }
}