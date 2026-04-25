using EcommerceApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/client/notifications")]
[Authorize(Roles = "Client")]
public class ClientNotificationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ClientNotificationsController(AppDbContext db) => _db = db;

    int ClientId()
    {
        var idStr =
            User.FindFirstValue("clientId") ??
            User.FindFirstValue("ClientId") ??
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub");

        return int.TryParse(idStr, out var id) ? id : 0;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var cid = ClientId();
        if (cid <= 0) return Unauthorized(new { ok = false, message = "ClientId introuvable dans le token." });

        var items = await _db.Notifications.AsNoTracking()
            .Where(n => n.CustomerId == cid)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(50)
            .Select(n => new {
                n.Id,
                n.Title,
                n.Message,
                n.Type,
                n.RefId,
                n.IsRead,
                n.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(new
        {
            ok = true,
            unreadCount = items.Count(x => !x.IsRead), // <- ou calcule côté SQL si tu veux
            items
        });
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var cid = ClientId();
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == cid);
        if (n == null) return NotFound(new { ok = false, message = "Notification introuvable." });

        n.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead()
    {
        var cid = ClientId();
        if (cid <= 0) return Unauthorized(new { ok = false, message = "ClientId introuvable dans le token." });

        // met tout en lu (uniquement les non lues)
        await _db.Notifications
            .Where(x => x.CustomerId == cid && !x.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true));

        return Ok(new { ok = true });
    }
}