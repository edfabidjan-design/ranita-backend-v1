using EcommerceApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace EcommerceApi.Service;

public class CommissionResolver
{
    private readonly AppDbContext _db;
    public CommissionResolver(AppDbContext db) => _db = db;

    // retourne un taux 0..1 (0.10 = 10%)
    public async Task<decimal> GetEffectiveRateAsync(int? categoryId)
    {
        var currentId = categoryId;

        while (currentId != null)
        {
            var cat = await _db.Categories.AsNoTracking()
                .Where(c => c.Id == currentId.Value)
                .Select(c => new { c.ParentId, c.CommissionRate })
                .FirstOrDefaultAsync();

            if (cat == null) break;

            if (cat.CommissionRate.HasValue && cat.CommissionRate.Value > 0m)
                return Normalize01(cat.CommissionRate.Value);

            currentId = cat.ParentId;
        }

        var raw = await _db.Settings.AsNoTracking()
            .Where(x => x.Key == "GlobalCommissionRate")
            .Select(x => x.Value)
            .FirstOrDefaultAsync();

        var r = Parse(raw);
        if (r > 0m) return Normalize01(r);

        return 0.10m; // fallback
    }

    public static decimal ComputeFee(decimal baseAmount, decimal rate01)
        => Math.Round(baseAmount * rate01, 2, MidpointRounding.AwayFromZero);

    private static decimal Normalize01(decimal v)
    {
        if (v > 1m) v /= 100m; // 10 => 0.10
        if (v < 0m) v = 0m;
        if (v > 1m) v = 1m;
        return v;
    }

    private static decimal Parse(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0m;

        var txt = s.Trim().Replace("%", "").Replace(" ", "").Replace(',', '.');
        return decimal.TryParse(txt, NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : 0m;
    }
}