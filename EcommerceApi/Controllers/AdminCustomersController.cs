using EcommerceApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/admin/customers")]
[Authorize]
public class AdminCustomersController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminCustomersController(AppDbContext db) { _db = db; }

    // GET /api/admin/customers?q=
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? q)
    {
        var customers = _db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            customers = customers.Where(c =>
                c.FullName.Contains(q) ||
                c.Phone.Contains(q) ||
                c.Email.Contains(q));
        }

        var data = await customers
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new
            {
                c.Id,
                c.FullName,
                c.Phone,
                c.Email,
                c.IsActive,
                c.CreatedAtUtc,
                OrdersCount = _db.Orders.Count(o => o.CustomerId == c.Id)
            })
            .ToListAsync();

        return Ok(new { ok = true, items = data });
    }

    // GET /api/admin/customers/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOne(int id)
    {
        var c = await _db.Customers.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.Phone,
                x.Email,
                x.IsActive,
                x.CreatedAtUtc
            })
            .FirstOrDefaultAsync();

        if (c == null) return NotFound(new { ok = false, message = "Client introuvable." });

        var orders = await _db.Orders.AsNoTracking()
            .Where(o => o.CustomerId == id)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.Id,
                o.Total,
                o.Status,
                o.CreatedAt
            })
            .ToListAsync();

        return Ok(new { ok = true, customer = c, orders });
    }

    // PUT /api/admin/customers/{id}/active
    // PUT /api/admin/customers/{id}/active
    [HttpPut("{id:int}/active")]
    public async Task<IActionResult> SetActive(int id, [FromBody] ActiveDto dto)
    {
        var c = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound(new { ok = false, message = "Client introuvable." });

        c.IsActive = dto.IsActive; // ✅ correspond à { isActive: true/false }
        await _db.SaveChangesAsync();

        return Ok(new { ok = true, message = dto.IsActive ? "Client réactivé." : "Client désactivé." });
    }

    public class ActiveDto
    {
        public bool IsActive { get; set; }
    }


    // DELETE /api/admin/customers/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var hasOrders = await _db.Orders.AnyAsync(o => o.CustomerId == id);
        if (hasOrders)
            return BadRequest(new { ok = false, message = "Suppression impossible : client avec commandes. Désactivez-le." });

        var c = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound(new { ok = false, message = "Client introuvable." });

        _db.Customers.Remove(c);
        await _db.SaveChangesAsync();

        return Ok(new { ok = true, message = "Client supprimé." });
    }

   
}
