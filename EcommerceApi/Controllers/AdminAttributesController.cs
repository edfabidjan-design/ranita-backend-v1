using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/admin/attributes")]
[Authorize]
public class AdminAttributesController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminAttributesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!await HasPermission("attributes.view"))
            return Forbid();

        var items = await _db.ProductAttributes
            .AsNoTracking()
            .Include(a => a.Options)
            .OrderBy(a => a.Name)
            .Select(a => new
            {
                a.Id,
                a.Code,
                a.Name,
                dataType = a.DataType.ToString(),
                a.IsVariant,
                a.IsActive,
                options = a.Options
                    .OrderBy(o => o.SortOrder)
                    .ThenBy(o => o.Value)
                    .Select(o => new { o.Id, o.Value, o.SortOrder, o.IsActive })
                    .ToList()
            })
            .ToListAsync();

        return Ok(new { ok = true, items });
    }

    public record CreateAttrDto(string? Code, string Name, string? DataType, bool IsVariant, bool IsActive = true);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAttrDto dto)
    {
        if (!await HasPermission("attributes.manage"))
            return Forbid();

        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { ok = false, message = "Name obligatoire." });

        var code = string.IsNullOrWhiteSpace(dto.Code)
            ? NormalizeCode(dto.Name)
            : NormalizeCode(dto.Code);

        var exists = await _db.ProductAttributes.AnyAsync(a => a.Code == code);
        if (exists)
            return BadRequest(new { ok = false, message = "Ce code d’attribut existe déjà." });

        var dtStr = (dto.DataType ?? "Text").Trim();
        if (!Enum.TryParse<AttributeDataType>(dtStr, true, out var dt))
            dt = AttributeDataType.Text;

        var ent = new ProductAttribute
        {
            Code = code,
            Name = dto.Name.Trim(),
            DataType = dt,
            IsVariant = dto.IsVariant,
            IsActive = dto.IsActive
        };

        _db.ProductAttributes.Add(ent);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException?.Message.Contains("IX_Attributes_Code", StringComparison.OrdinalIgnoreCase) == true
             || ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true)
            {
                return BadRequest(new { ok = false, message = "Ce code d’attribut existe déjà." });
            }

            return BadRequest(new { ok = false, message = "Erreur base de données." });
        }

        return Ok(new { ok = true, id = ent.Id });
    }

    public record AddOptionDto(string Value, int SortOrder = 0, bool IsActive = true);

    [HttpPost("{id:int}/options")]
    public async Task<IActionResult> AddOption(int id, [FromBody] AddOptionDto dto)
    {
        if (!await HasPermission("attributes.manage"))
            return Forbid();

        var attr = await _db.ProductAttributes.FirstOrDefaultAsync(a => a.Id == id);
        if (attr == null) return NotFound(new { ok = false, message = "Attribut introuvable." });

        if (attr.DataType != AttributeDataType.Option)
            return BadRequest(new { ok = false, message = "Cet attribut n'est pas de type Option." });

        var val = (dto?.Value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(val))
            return BadRequest(new { ok = false, message = "Value obligatoire." });

        var exists = await _db.ProductAttributeOptions
            .AnyAsync(o => o.ProductAttributeId == id && o.Value.ToLower() == val.ToLower());

        if (exists)
            return BadRequest(new { ok = false, message = $"Option déjà existante : {val}" });

        _db.ProductAttributeOptions.Add(new ProductAttributeOption
        {
            ProductAttributeId = id,
            Value = val,
            SortOrder = dto?.SortOrder ?? 0,
            IsActive = dto?.IsActive ?? true
        });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { ok = false, message = $"Option déjà existante : {val}" });
        }

        return Ok(new { ok = true });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOne(int id)
    {
        if (!await HasPermission("attributes.view"))
            return Forbid();

        var a = await _db.ProductAttributes
            .AsNoTracking()
            .Include(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (a == null) return NotFound(new { ok = false, message = "Attribut introuvable." });

        return Ok(new
        {
            ok = true,
            item = new
            {
                a.Id,
                a.Code,
                a.Name,
                dataType = a.DataType.ToString(),
                a.IsVariant,
                a.IsActive,
                options = a.Options
                    .OrderBy(o => o.SortOrder)
                    .ThenBy(o => o.Value)
                    .Select(o => new { o.Id, o.Value, o.SortOrder, o.IsActive })
                    .ToList()
            }
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateAttrDto dto)
    {
        if (!await HasPermission("attributes.manage"))
            return Forbid();

        var ent = await _db.ProductAttributes.FirstOrDefaultAsync(x => x.Id == id);
        if (ent == null) return NotFound(new { ok = false, message = "Attribut introuvable." });

        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { ok = false, message = "Name obligatoire." });

        var code = string.IsNullOrWhiteSpace(dto.Code)
            ? NormalizeCode(dto.Name)
            : NormalizeCode(dto.Code);

        var exists = await _db.ProductAttributes.AnyAsync(a => a.Id != id && a.Code == code);
        if (exists)
            return BadRequest(new { ok = false, message = "Ce code d’attribut existe déjà." });

        var dtStr = (dto.DataType ?? "Text").Trim();
        if (!Enum.TryParse<AttributeDataType>(dtStr, true, out var dt))
            dt = AttributeDataType.Text;

        ent.Code = code;
        ent.Name = dto.Name.Trim();
        ent.DataType = dt;
        ent.IsVariant = dto.IsVariant;
        ent.IsActive = dto.IsActive;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException?.Message.Contains("IX_Attributes_Code", StringComparison.OrdinalIgnoreCase) == true
             || ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true)
            {
                return BadRequest(new { ok = false, message = "Ce code d’attribut existe déjà." });
            }

            return BadRequest(new { ok = false, message = "Erreur base de données." });
        }

        return Ok(new { ok = true });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await HasPermission("attributes.manage"))
            return Forbid();

        var ent = await _db.ProductAttributes
            .Include(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (ent == null) return NotFound(new { ok = false, message = "Attribut introuvable." });

        _db.ProductAttributeOptions.RemoveRange(ent.Options);
        _db.ProductAttributes.Remove(ent);

        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpPut("{id:int}/options/{optId:int}")]
    public async Task<IActionResult> UpdateOption(int id, int optId, [FromBody] AddOptionDto dto)
    {
        if (!await HasPermission("attributes.manage"))
            return Forbid();

        var opt = await _db.ProductAttributeOptions
            .FirstOrDefaultAsync(o => o.Id == optId && o.ProductAttributeId == id);

        if (opt == null) return NotFound(new { ok = false, message = "Option introuvable." });

        var val = (dto?.Value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(val))
            return BadRequest(new { ok = false, message = "Value obligatoire." });

        opt.Value = val;
        opt.SortOrder = dto?.SortOrder ?? 0;
        opt.IsActive = dto?.IsActive ?? true;

        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpDelete("{id:int}/options/{optId:int}")]
    public async Task<IActionResult> DeleteOption(int id, int optId)
    {
        if (!await HasPermission("attributes.manage"))
            return Forbid();

        var opt = await _db.ProductAttributeOptions
            .FirstOrDefaultAsync(o => o.Id == optId && o.ProductAttributeId == id);

        if (opt == null) return NotFound(new { ok = false, message = "Option introuvable." });

        _db.ProductAttributeOptions.Remove(opt);
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    private static string NormalizeCode(string? codeOrName)
    {
        var s = (codeOrName ?? "").Trim().ToLowerInvariant();
        s = s.Replace(" ", "_");
        while (s.Contains("__")) s = s.Replace("__", "_");
        return s.Trim('_');
    }

    private async Task<bool> HasPermission(string code)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
            return false;

        var user = await _db.Users
            .Include(u => u.RoleRef)!
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        if (user == null || user.RoleRef == null)
            return false;

        if (string.Equals(user.RoleRef.Code, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase))
            return true;

        return user.RoleRef.RolePermissions.Any(rp => rp.Permission.Code == code);
    }


}