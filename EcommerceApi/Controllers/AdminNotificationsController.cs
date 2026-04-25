using EcommerceApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/admin/notifications")]
[Authorize(Roles = "SUPER_ADMIN,ADMIN,CATALOGUE_MANAGER")]
public class AdminNotificationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminNotificationsController(AppDbContext db) => _db = db;

    [HttpGet("latest-unread")]
    public async Task<IActionResult> LatestUnread()
    {
        var n = await _db.AdminNotifications.AsNoTracking()
            .Where(x => !x.IsRead)
            .OrderByDescending(x => x.Id)
            .Select(x => new { x.Id, x.Type, x.RefId, x.Title, x.Message, x.CreatedAtUtc })
            .FirstOrDefaultAsync();

        return Ok(new { ok = true, item = n });
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var n = await _db.AdminNotifications.FirstOrDefaultAsync(x => x.Id == id);
        if (n == null) return NotFound(new { ok = false });

        n.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }


}