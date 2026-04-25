using EcommerceApi.Data;
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Service;

namespace EcommerceApi.Service;

public class OrderPaymentFinalizer
{
    private readonly AppDbContext _db;
    private readonly CommissionResolver _commission;

    public OrderPaymentFinalizer(AppDbContext db, CommissionResolver commission)
    {
        _db = db;
        _commission = commission;
    }

    public async Task FinalizePaidOrderAsync(int orderId, string paymentRef)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) throw new Exception("Order introuvable.");
        if (order.IsPaid) return;

        // Charge les produits (Vendor plus nécessaire pour la commission)
        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();

        var products = await _db.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.CategoryId
            })
            .ToListAsync();

        // ✅ Cache des taux par catégorie (évite appels répétés)
        var categoryIds = products.Select(p => p.CategoryId).Distinct().ToList();
        var rateByCategory = new Dictionary<int, decimal>(); // rate01 (0..1)

        foreach (var cid in categoryIds)
            rateByCategory[cid] = await _commission.GetEffectiveRateAsync(cid);

        foreach (var it in order.Items)
        {
            var p = products.FirstOrDefault(x => x.Id == it.ProductId);
            if (p == null) continue;

            var rate01 = rateByCategory.TryGetValue(p.CategoryId, out var r) ? r : 0m;

            // line total
            var lineTotal = it.UnitPriceSnapshot * it.Quantity;

            // ✅ fee = lineTotal * rate01 (rate01 = 0.12 pour 12%)
            var fee = Math.Round(lineTotal * rate01, 2, MidpointRounding.AwayFromZero);

            var vendorNet = lineTotal - fee;

            // ✅ On garde ton snapshot en POURCENT (comme avant)
            var ratePct = Math.Round(rate01 * 100m, 4); // ex: 12.0000

            // ✅ fige les champs
            it.CommissionRateSnapshot = ratePct;   // ex: 12
            it.CommissionAmount = fee;
            it.PlatformFee = fee;

            it.VendorNetAmount = vendorNet;
            it.VendorAmount = vendorNet;           // vendeur net
        }

        // marque payé
        order.IsPaid = true;
        order.PaidAt = DateTime.UtcNow;
        order.PaymentRef = paymentRef;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}