using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/vendor/payments")]
[Authorize(Roles = "Vendor")]
public class VendorPaymentsController : ControllerBase
{
    private readonly AppDbContext _db;
    public VendorPaymentsController(AppDbContext db) => _db = db;

    // ✅ IMPORTANT: on récupère vendorId (pas NameIdentifier)
    private int VendorId
    {
        get
        {
            var s = User.FindFirstValue("vendorId");
            if (string.IsNullOrWhiteSpace(s) || !int.TryParse(s, out var id) || id <= 0)
                throw new Exception("vendorId introuvable dans le token (claim vendorId).");
            return id;
        }
    }

    private static (DateTime startUtc, DateTime endUtc) WeekPeriodUtc(int weeksAgo)
    {
        var now = DateTime.UtcNow;
        var today = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);

        int diff = ((int)today.DayOfWeek + 6) % 7; // Monday=0
        var start = today.AddDays(-diff).AddDays(-7 * weeksAgo);
        var end = start.AddDays(7).AddTicks(-1);
        return (start, end);
    }

    static bool IsDeliveredCredit(string t) =>
    t == VendorWalletTxTypes.DeliveredCredit || t == "Delivered" || t == "Credit";

    static bool IsPayout(string t) =>
        t == VendorWalletTxTypes.Payout || t == "Payout";

    static bool IsRefundDebit(string t) =>
        t == VendorWalletTxTypes.RefundDebit || t.StartsWith("Refund", StringComparison.OrdinalIgnoreCase);
    // GET /api/vendor/payments?weeksAgo=0
    // GET /api/vendor/payments?weeksAgo=0
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int weeksAgo = 0, CancellationToken ct = default)
    {
        var vendorId = VendorId;
        var (startOfWeek, endOfWeek) = WeekPeriodUtc(weeksAgo);

        // 🟢 TOTAL GAINS GENERES (Delivered)
        // ✅ Totaux semaine (wallet)
        var weekTx = _db.VendorWalletTransactions
            .AsNoTracking()
            .Where(x =>
                x.VendorId == vendorId &&
                x.CreatedAt >= startOfWeek &&
                x.CreatedAt <= endOfWeek);

        // Crédit = DeliveredCredit + Adjustment positif (si tu en as)
        var totalCredit = await weekTx
            .Where(x =>
                x.Type == VendorWalletTxTypes.DeliveredCredit ||
                x.Type == "Delivered" ||
                x.Type == "Credit" ||
                x.Type == VendorWalletTxTypes.Adjustment)
            .Where(x => x.Amount > 0)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;


        var totalDebit = await weekTx
            .Where(x =>
                x.Type == VendorWalletTxTypes.RefundDebit ||
                x.Type.StartsWith("Refund") ||
                x.Type == VendorWalletTxTypes.Payout ||
                x.Type == "Payout" ||
                x.Type == VendorWalletTxTypes.Adjustment)
            .Where(x => x.Amount < 0)
            .SumAsync(x => (decimal?)-x.Amount) ?? 0m;


        var totalNet = await weekTx.SumAsync(x => (decimal?)x.Amount) ?? 0m;

        var items = await weekTx
            .Where(x =>
                x.Type == VendorWalletTxTypes.DeliveredCredit ||
                x.Type == "Delivered" ||
                x.Type == "Credit" ||
                x.Type == VendorWalletTxTypes.Payout ||
                x.Type == "Payout" ||
                x.Type == VendorWalletTxTypes.RefundDebit ||
                x.Type.StartsWith("Refund"))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                id = x.Id,
                amount = x.Amount,
                type = x.Type,
                reference = x.Reference,
                createdAt = x.CreatedAt
            })
            .ToListAsync(ct);



        return Ok(new
        {
            ok = true,
            periodStart = startOfWeek,
            periodEnd = endOfWeek,
            totalCredit,
            totalDebit,
            totalNet,
            items
        });
    }
    // GET /api/vendor/payments/123
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        int vendorId;
        try
        {
            vendorId = VendorId; // ✅ utilise la propriété
        }
        catch
        {
            return Unauthorized(new { ok = false, message = "vendorId introuvable dans le token." });
        }

        var tx = await _db.VendorWalletTransactions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.VendorId == vendorId, ct);

        if (tx == null) return NotFound();

        object? oi = null;

        var m = Regex.Match(tx.Reference ?? "", @"(?:OrderItem|orderItem):(\d+)", RegexOptions.IgnoreCase);

        if (m.Success && int.TryParse(m.Groups[1].Value, out var orderItemId))
        {
            oi = await _db.OrderItems.AsNoTracking()
                .Include(x => x.Order)
                .Include(x => x.Product).ThenInclude(p => p.Images)
                .Where(x => x.Id == orderItemId && x.VendorId == vendorId)
                .Select(x => new
                {
                    x.Id,
                    x.OrderId,
                    x.ProductId,
                    x.ProductName,
                    Quantity = x.Quantity,
                    UnitPriceSnapshot = x.UnitPriceSnapshot,
                    PlatformFee = x.PlatformFee,
                    VendorAmount = x.VendorAmount,
                    DeliveredAt = x.DeliveredAt,

                    ProductImage = x.Product.Images
                        .OrderBy(i => i.SortOrder)
                        .Select(i => i.Url)
                        .FirstOrDefault(),

                    BuyerName = x.Order != null ? x.Order.FullName : null,
                    BuyerAddress = x.Order != null
                        ? (x.Order.Address + (x.Order.City != "" ? (", " + x.Order.City) : ""))
                        : null
                })
                .FirstOrDefaultAsync(ct);
        }

        return Ok(new
        {
            tx.Id,
            tx.Amount,
            tx.Type,
            tx.Reference,
            tx.CreatedAt,
            orderItem = oi
        });
    }

    public record OrderItemDto(
        int Id,
        int OrderId,
        int ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPriceSnapshot,
        decimal PlatformFee,
        decimal VendorAmount,
        DateTime? DeliveredAt
    );
}