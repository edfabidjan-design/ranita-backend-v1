using EcommerceApi.Data;
using EcommerceApi.Dtos;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/admin/commissions")]
[Authorize]
public class AdminCommissionsController : ControllerBase
{
    private static readonly string[] CommissionTypes = { "Commission", "CommissionReversal" };

    private readonly AppDbContext _db;
    public AdminCommissionsController(AppDbContext db) => _db = db;

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

    [HttpGet("global")]
    public async Task<ActionResult<object>> GetGlobal()
    {
        if (!await HasPermission("commissions.view"))
            return Forbid();

        var s = await _db.Settings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == "GlobalCommissionRate");

        var rate = 0m;

        if (!string.IsNullOrWhiteSpace(s?.Value))
        {
            var txt = s.Value.Trim()
                .Replace("%", "")
                .Replace(" ", "")
                .Replace(',', '.');

            if (decimal.TryParse(txt, NumberStyles.Any, CultureInfo.InvariantCulture, out var r))
                rate = r;
        }

        return Ok(new { rate, percent = rate * 100m });
    }

    [HttpPut("global")]
    public async Task<IActionResult> SetGlobal([FromBody] GlobalCommissionDto dto)
    {
        if (!await HasPermission("commissions.manage"))
            return Forbid();

        if (dto.Percent < 0 || dto.Percent > 100)
            return BadRequest("Percent doit être entre 0 et 100.");

        var rate = Math.Round(dto.Percent / 100m, 4);

        var s = await _db.Settings.FirstOrDefaultAsync(x => x.Key == "GlobalCommissionRate");
        if (s == null)
        {
            _db.Settings.Add(new Setting
            {
                Key = "GlobalCommissionRate",
                Value = rate.ToString("0.####"),
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            s.Value = rate.ToString("0.####");
            s.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("category/{categoryId:int}")]
    public async Task<IActionResult> SetCategoryRate(int categoryId, [FromBody] CategoryCommissionUpsertDto dto)
    {
        if (!await HasPermission("commissions.manage"))
            return Forbid();

        if (dto.Percent < 0 || dto.Percent > 100)
            return BadRequest("Percent doit être entre 0 et 100.");

        var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId);
        if (cat == null) return NotFound("Catégorie introuvable.");

        if (!dto.IsActive) cat.CommissionRate = null;
        else cat.CommissionRate = Math.Round(dto.Percent / 100m, 4);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("category/{categoryId:int}")]
    public async Task<ActionResult<object>> GetCategoryRate(int categoryId)
    {
        if (!await HasPermission("commissions.view"))
            return Forbid();

        var cat = await _db.Categories.AsNoTracking()
            .Select(c => new { c.Id, c.CommissionRate, c.ParentId })
            .FirstOrDefaultAsync(c => c.Id == categoryId);

        if (cat == null) return NotFound();

        return Ok(new
        {
            categoryId = cat.Id,
            hasRate = cat.CommissionRate.HasValue,
            rate = cat.CommissionRate,
            percent = cat.CommissionRate.HasValue ? cat.CommissionRate.Value * 100m : (decimal?)null,
            parentId = cat.ParentId
        });
    }

    [HttpGet("weekly")]
    public async Task<IActionResult> GetWeekly([FromQuery] DateTime start, [FromQuery] DateTime end)
    {
        if (!await HasPermission("commissions.view"))
            return Forbid();

        if (end < start) return BadRequest("end doit être >= start.");

        var startDate = start.Date;
        var endDateExclusive = end.Date.AddDays(1);

        var details = await _db.AdminWalletTransactions
            .AsNoTracking()
            .Where(t => CommissionTypes.Contains(t.Type)
                && t.OrderItemId != null
                && t.CreatedAt >= startDate
                && t.CreatedAt < endDateExclusive)
            .Join(_db.OrderItems.Include(o => o.Product).ThenInclude(p => p.Vendor),
                t => t.OrderItemId,
                o => o.Id,
                (t, o) => new
                {
                    t.Id,
                    t.Amount,
                    t.CreatedAt,
                    t.OrderId,
                    t.OrderItemId,
                    t.Note,
                    VendorShopName = o.Product.Vendor.ShopName
                })
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(new
        {
            periodStart = startDate,
            periodEnd = endDateExclusive.AddTicks(-1),
            totalCommission = details.Sum(x => x.Amount),
            totalOrders = details.Select(x => x.OrderId).Where(x => x != null).Distinct().Count(),
            count = details.Count,
            details
        });
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] int weeks = 8)
    {
        if (!await HasPermission("commissions.view"))
            return Forbid();

        if (weeks < 1) weeks = 1;
        if (weeks > 26) weeks = 26;

        var now = DateTime.UtcNow;

        var adminBalance = await _db.AdminWalletTransactions
            .AsNoTracking()
            .Where(x => CommissionTypes.Contains(x.Type))
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthTotal = await _db.AdminWalletTransactions
            .AsNoTracking()
            .Where(x => CommissionTypes.Contains(x.Type)
                && x.CreatedAt >= monthStart
                && x.CreatedAt <= now)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        var todayStart = now.Date;
        var todayEnd = todayStart.AddDays(1);

        var todayTotal = await _db.AdminWalletTransactions
            .AsNoTracking()
            .Where(x => CommissionTypes.Contains(x.Type)
                && x.CreatedAt >= todayStart
                && x.CreatedAt < todayEnd)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        static DateTime StartOfWeekMonday(DateTime utc)
        {
            int diff = ((int)utc.DayOfWeek + 6) % 7;
            var d = utc.Date.AddDays(-diff);
            return DateTime.SpecifyKind(d, DateTimeKind.Utc);
        }

        var currentWeekStart = StartOfWeekMonday(now);
        var from = currentWeekStart.AddDays(-(weeks - 1) * 7);
        var toExclusive = currentWeekStart.AddDays(7);

        var rows = await _db.AdminWalletTransactions
            .AsNoTracking()
            .Where(x => CommissionTypes.Contains(x.Type)
                && x.CreatedAt >= from
                && x.CreatedAt < toExclusive)
            .Select(x => new { x.CreatedAt, x.Amount, x.OrderId })
            .ToListAsync();

        var map = rows
            .GroupBy(x => StartOfWeekMonday(x.CreatedAt))
            .ToDictionary(g => g.Key, g => new
            {
                weekStart = g.Key,
                weekEnd = g.Key.AddDays(7).AddTicks(-1),
                totalCommission = g.Sum(x => x.Amount),
                totalOrders = g.Select(x => x.OrderId).Where(id => id != null).Distinct().Count(),
                countLines = g.Count()
            });

        var filled = new List<object>();
        for (int i = 0; i < weeks; i++)
        {
            var ws = from.AddDays(i * 7);
            if (map.TryGetValue(ws, out var agg)) filled.Add(agg);
            else
            {
                filled.Add(new
                {
                    weekStart = ws,
                    weekEnd = ws.AddDays(7).AddTicks(-1),
                    totalCommission = 0m,
                    totalOrders = 0,
                    countLines = 0
                });
            }
        }

        return Ok(new
        {
            now,
            adminBalance,
            monthStart,
            monthTotal,
            todayTotal,
            weeks,
            weeklySeries = filled
        });
    }

    [HttpGet("range")]
    public async Task<IActionResult> GetRange([FromQuery] DateTime start, [FromQuery] DateTime end)
    {
        if (!await HasPermission("commissions.view"))
            return Forbid();

        if (end < start) return BadRequest("end doit être >= start.");

        var startDate = start.Date;
        var endDateExclusive = end.Date.AddDays(1);

        static DateTime StartOfWeekMonday(DateTime d)
        {
            int diff = ((int)d.DayOfWeek + 6) % 7;
            return d.Date.AddDays(-diff);
        }

        var firstWeek = StartOfWeekMonday(startDate);
        var lastWeek = StartOfWeekMonday(endDateExclusive.AddTicks(-1));

        var rows = await _db.AdminWalletTransactions
            .AsNoTracking()
            .Where(x => CommissionTypes.Contains(x.Type)
                && x.CreatedAt >= startDate
                && x.CreatedAt < endDateExclusive)
            .Select(x => new { x.CreatedAt, x.Amount, x.OrderId })
            .ToListAsync();

        var grouped = rows
            .GroupBy(x => StartOfWeekMonday(x.CreatedAt))
            .ToDictionary(g => g.Key, g => new
            {
                weekStart = g.Key,
                weekEnd = g.Key.AddDays(7).AddTicks(-1),
                totalCommission = g.Sum(x => x.Amount),
                totalOrders = g.Select(x => x.OrderId).Where(id => id != null).Distinct().Count(),
                countLines = g.Count()
            });

        var series = new List<object>();
        for (var ws = firstWeek; ws <= lastWeek; ws = ws.AddDays(7))
        {
            if (grouped.TryGetValue(ws, out var found)) series.Add(found);
            else
            {
                series.Add(new
                {
                    weekStart = ws,
                    weekEnd = ws.AddDays(7).AddTicks(-1),
                    totalCommission = 0m,
                    totalOrders = 0,
                    countLines = 0
                });
            }
        }

        return Ok(new
        {
            periodStart = startDate,
            periodEnd = endDateExclusive.AddTicks(-1),
            totalCommission = rows.Sum(x => x.Amount),
            totalOrders = rows.Select(x => x.OrderId).Where(x => x != null).Distinct().Count(),
            countLines = rows.Count,
            weeklySeries = series
        });
    }

    [HttpGet("range-daily")]
    public async Task<IActionResult> GetRangeDaily([FromQuery] DateTime start, [FromQuery] DateTime end)
    {
        if (!await HasPermission("commissions.view"))
            return Forbid();

        if (end < start) return BadRequest("end doit être >= start.");

        var startDate = start.Date;
        var endDateExclusive = end.Date.AddDays(1);

        var rows = await _db.AdminWalletTransactions
            .AsNoTracking()
            .Where(x => CommissionTypes.Contains(x.Type)
                && x.CreatedAt >= startDate
                && x.CreatedAt < endDateExclusive)
            .Select(x => new { x.CreatedAt, x.Amount, x.OrderId })
            .ToListAsync();

        var grouped = rows
            .GroupBy(x => x.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => new
            {
                day = g.Key,
                totalCommission = g.Sum(x => x.Amount),
                totalOrders = g.Select(x => x.OrderId).Where(id => id != null).Distinct().Count(),
                countLines = g.Count()
            });

        var series = new List<object>();
        for (var d = startDate; d < endDateExclusive; d = d.AddDays(1))
        {
            if (grouped.TryGetValue(d, out var found)) series.Add(found);
            else
            {
                series.Add(new
                {
                    day = d,
                    totalCommission = 0m,
                    totalOrders = 0,
                    countLines = 0
                });
            }
        }

        return Ok(new
        {
            periodStart = startDate,
            periodEnd = endDateExclusive.AddTicks(-1),
            totalCommission = rows.Sum(x => x.Amount),
            totalOrders = rows.Select(x => x.OrderId).Where(x => x != null).Distinct().Count(),
            countLines = rows.Count,
            dailySeries = series
        });
    }
}