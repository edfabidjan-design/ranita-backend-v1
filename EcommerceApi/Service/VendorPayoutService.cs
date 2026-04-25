using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Service;

public class VendorPayoutService
{
    private readonly AppDbContext _db;

    public VendorPayoutService(AppDbContext db)
    {
        _db = db;
    }

    public static (DateTime start, DateTime end) GetPreviousWeekPeriodUtc(DateTime nowUtc)
    {
        // nowUtc = UTC = GMT (Abidjan)
        // On veut la semaine précédente : lundi 00:00 -> dimanche 23:59:59
        // On se place sur le lundi de la semaine en cours, puis -7 jours
        int diff = ((int)nowUtc.DayOfWeek + 6) % 7; // Monday=0 ... Sunday=6
        var thisMonday = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, DateTimeKind.Utc)
            .AddDays(-diff);

        var start = thisMonday.AddDays(-7);
        var end = thisMonday.AddTicks(-1); // dimanche 23:59:59.9999999

        return (start, end);
    }

    public async Task<int?> GenerateWeeklyBatchAsync(DateTime periodStartUtc, DateTime periodEndUtc, CancellationToken ct)
    {
        // idempotence
        var exists = await _db.VendorPayoutBatches
            .AnyAsync(x => x.PeriodStart == periodStartUtc && x.PeriodEnd == periodEndUtc, ct);

        if (exists) return null;

        // ventes éligibles : DeliveredAt dans la période + non payées
        var items = await _db.OrderItems
            .Include(i => i.Order)
            .Include(i => i.Vendor).ThenInclude(v => v.Account)
            .Where(i =>
                i.VendorStatus == "Delivered" &&
                i.DeliveredAt != null &&
                i.DeliveredAt >= periodStartUtc &&
                i.DeliveredAt <= periodEndUtc &&
                !i.IsVendorPaid &&
                i.VendorPayoutId == null)
            .ToListAsync(ct);

        if (items.Count == 0) return null;

        var batch = new VendorPayoutBatch
        {
            PeriodStart = periodStartUtc,
            PeriodEnd = periodEndUtc,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        // group vendor
        var groups = items.GroupBy(x => x.VendorId).ToList();

        foreach (var g in groups)
        {
            var vendorId = g.Key;
            var amount = g.Sum(x => x.VendorAmount);

            var payout = new VendorPayout
            {
                VendorId = vendorId,
                Amount = amount,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            // détail
            foreach (var it in g)
            {
                var lineTotal = it.UnitPriceSnapshot * it.Quantity;

                payout.Sales.Add(new VendorPayoutSale
                {
                    OrderId = it.OrderId,
                    OrderItemId = it.Id,
                    ProductId = it.ProductId,
                    ProductName = it.ProductName,
                    Qty = it.Quantity,
                    UnitPrice = it.UnitPriceSnapshot,
                    LineTotal = lineTotal,
                    PlatformFee = it.PlatformFee,
                    VendorAmount = it.VendorAmount,
                    SoldAt = it.Order?.CreatedAt ?? DateTime.UtcNow,
                    DeliveredAt = it.DeliveredAt
                });
            }

            batch.Payouts.Add(payout);
        }

        batch.TotalVendors = batch.Payouts.Count;
        batch.TotalAmount = batch.Payouts.Sum(p => p.Amount);

        _db.VendorPayoutBatches.Add(batch);
        await _db.SaveChangesAsync(ct);

        // IMPORTANT : on relie les OrderItems à leur payout (VendorPayoutId)
        // On recharge les payouts pour avoir leurs Id
        await _db.Entry(batch).Collection(x => x.Payouts).LoadAsync(ct);

        var payoutByVendor = batch.Payouts.ToDictionary(x => x.VendorId, x => x.Id);
        foreach (var it in items)
        {
            it.VendorPayoutId = payoutByVendor[it.VendorId];
        }

        await _db.SaveChangesAsync(ct);
        return batch.Id;
    }

    // Ici : on "paye" en créditant le wallet vendor (comme ton code actuel)
    public async Task ProcessBatchToWalletAsync(int batchId, CancellationToken ct)
    {
        var batch = await _db.VendorPayoutBatches
            .Include(b => b.Payouts).ThenInclude(p => p.Vendor).ThenInclude(v => v.Account)
            .FirstOrDefaultAsync(b => b.Id == batchId, ct);

        if (batch == null) return;
        if (batch.Status == "Paid") return;

        var now = DateTime.UtcNow;
        batch.Status = "Processing";

        await _db.SaveChangesAsync(ct);

        foreach (var payout in batch.Payouts)
        {
            try
            {
                if (payout.Vendor?.Account == null)
                {
                    payout.Vendor!.Account = new VendorAccount
                    {
                        VendorId = payout.VendorId,
                        Email = payout.Vendor.Email,
                        PasswordHash = "",
                        IsActive = true,
                        WalletBalance = 0m
                    };
                }

                payout.Vendor!.Account!.WalletBalance += payout.Amount;

                payout.Status = "Paid";
                payout.PaidAt = now;
                payout.PaymentRef = $"BATCH:{batch.Id}-VENDOR:{payout.VendorId}";

                // marque tous les OrderItems du payout comme payés
                var itemIds = await _db.VendorPayoutSales
                    .Where(s => s.VendorPayoutId == payout.Id)
                    .Select(s => s.OrderItemId)
                    .ToListAsync(ct);

                var orderItems = await _db.OrderItems.Where(i => itemIds.Contains(i.Id)).ToListAsync(ct);
                foreach (var it in orderItems)
                {
                    it.IsVendorPaid = true;
                    it.VendorPaidAt = now;
                }

                _db.VendorWalletTransactions.Add(new VendorWalletTransaction
                {
                    VendorId = payout.VendorId,
                    Amount = payout.Amount,
                    Type = "Payout",
                    Reference = payout.PaymentRef,
                    CreatedAt = now
                });
            }
            catch (Exception ex)
            {
                payout.Status = "Failed";
                payout.FailureReason = ex.Message;
            }
        }

        // statut batch
        batch.PaidAt = now;
        batch.Status = batch.Payouts.All(p => p.Status == "Paid") ? "Paid" : "Failed";

        await _db.SaveChangesAsync(ct);
    }
}