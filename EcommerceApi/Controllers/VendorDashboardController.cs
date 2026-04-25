using EcommerceApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/vendor/dashboard")]
[Authorize(Roles = "Vendor")]
public class VendorDashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    public VendorDashboardController(AppDbContext db) => _db = db;

    private int VendorId
    {
        get
        {
            var s = User.FindFirstValue("VendorId") ?? User.FindFirstValue("vendorId");
            if (string.IsNullOrWhiteSpace(s) || !int.TryParse(s, out var id))
                throw new Exception("VendorId manquant dans le token.");
            return id;
        }
    }

    // ✅ GET /api/vendor/dashboard/summary
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var startToday = now.Date; // UTC
        var startWeek = startToday.AddDays(-((int)startToday.DayOfWeek + 6) % 7); // lundi
        var startMonth = new DateTime(now.Year, now.Month, 1);

        // ✅ EXACTEMENT comme tes requêtes SSMS
        var q = _db.VendorWalletTransactions.AsNoTracking()
            .Where(x => x.VendorId == VendorId)
            .Where(x => x.Type != "Payout"); // exclude payouts only

        var todayRevenue = await q
            .Where(x => x.CreatedAt >= startToday && x.CreatedAt < startToday.AddDays(1))
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

        var weekRevenue = await q
            .Where(x => x.CreatedAt >= startWeek && x.CreatedAt < startWeek.AddDays(7))
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

        var monthRevenue = await q
            .Where(x => x.CreatedAt >= startMonth && x.CreatedAt < startMonth.AddMonths(1))
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

        var totalRevenue = await q
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

        var pendingOrders = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.VendorId == VendorId)
            .Where(oi => oi.Order != null && oi.Order.Status == "EnAttente")
            .Select(oi => oi.OrderId)
            .Distinct()
            .CountAsync(ct);

        return Ok(new
        {
            todayRevenue,
            weekRevenue,
            monthRevenue,
            totalRevenue,
            pendingOrders
        });
    }
}