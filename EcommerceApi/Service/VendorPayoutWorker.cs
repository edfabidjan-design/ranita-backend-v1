using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Service.Background;

public class VendorPayoutWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public VendorPayoutWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoWork(stoppingToken);
            }
            catch
            {
                // log si tu veux
            }
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            // await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }

    private static DateTime StartOfWeekMonday(DateTime utc)
    {
        int diff = ((int)utc.DayOfWeek + 6) % 7; // Monday=0 ... Sunday=6
        var d = utc.Date.AddDays(-diff);
        return DateTime.SpecifyKind(d, DateTimeKind.Utc);
    }

    private async Task DoWork(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;

        var due = await db.OrderItems
            .Include(x => x.Order) // ✅ AJOUTE ÇA
            .Include(x => x.Vendor).ThenInclude(v => v.Account)
.Where(x =>
    x.Order != null &&
    x.Order.IsPaid == true &&   // ✅ IMPORTANT
    x.VendorStatus == "Delivered" &&
    !x.IsVendorPaid &&
    x.VendorPaidAt == null &&
    x.VendorPayableAt != null &&
    x.VendorPayableAt <= now
)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

        if (due.Count == 0) return;

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            // ✅ Batch de la semaine (lundi → dimanche)
            var periodStart = StartOfWeekMonday(now);
            var periodEnd = periodStart.AddDays(7).AddSeconds(-1);

            var batch = await db.VendorPayoutBatches
                .FirstOrDefaultAsync(b => b.PeriodStart == periodStart && b.PeriodEnd == periodEnd, ct);

            if (batch == null)
            {
                batch = new VendorPayoutBatch
                {
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    Status = "Pending",
                    CreatedAt = now
                };
                db.VendorPayoutBatches.Add(batch);
                await db.SaveChangesAsync(ct); // pour avoir batch.Id
            }

            // ✅ Group par vendeur
            foreach (var group in due.GroupBy(x => x.VendorId))
            {
                var vendorId = group.Key;

                // ✅ garantir VendorAccount
                var first = group.First();
                if (first.Vendor?.Account == null)
                {
                    var acc = new VendorAccount
                    {
                        VendorId = vendorId,
                        Email = first.Vendor?.Email ?? "",
                        PasswordHash = "",
                        IsActive = true,
                        WalletBalance = 0m
                    };
                    db.VendorAccounts.Add(acc);
                    first.Vendor!.Account = acc;
                    await db.SaveChangesAsync(ct);
                }

                // ✅ Payout du vendeur dans ce batch
                var payout = await db.VendorPayouts
                    .Include(p => p.Sales)
                    .FirstOrDefaultAsync(p => p.BatchId == batch.Id && p.VendorId == vendorId, ct);

                if (payout == null)
                {
                    payout = new VendorPayout
                    {
                        BatchId = batch.Id,
                        VendorId = vendorId,
                        Amount = 0m,
                        Status = "Paid",     // on crédite maintenant le wallet (donc payé)
                        CreatedAt = now,
                        PaidAt = now
                    };
                    db.VendorPayouts.Add(payout);
                    await db.SaveChangesAsync(ct); // pour payout.Id
                }

                foreach (var it in group)
                {
                    // ✅ anti-doublon: si déjà associé à un payout, skip
                    if (it.IsVendorPaid || it.VendorPaidAt != null || it.VendorPayoutId != null)
                        continue;

                    var amount = it.VendorNetAmount ?? it.VendorAmount;

                    // ✅ anti-doublon “sale”
                    var existsSale = await db.VendorPayoutSales
                        .AnyAsync(s => s.VendorPayoutId == payout.Id && s.OrderItemId == it.Id, ct);

                    if (existsSale)
                    {
                        it.IsVendorPaid = true;
                        it.VendorPaidAt = now;
                        it.VendorPayoutId = payout.Id;
                        continue;
                    }

                    // ✅ crédit wallet
                    first.Vendor!.Account!.WalletBalance += amount;

                    // ✅ lier item -> payout
                    it.IsVendorPaid = true;
                    it.VendorPaidAt = now;
                    it.VendorPayoutId = payout.Id;

                    // ✅ ajouter sale
                    var refKey = $"OrderItem:{it.Id}";

                    // ✅ Transaction wallet (ce que la page vendeur affiche)
                    var alreadyTx = await db.VendorWalletTransactions
                        .AnyAsync(t => t.VendorId == vendorId && t.Reference == refKey && t.Type == "Credit", ct);

                    if (!alreadyTx)
                    {
                        db.VendorWalletTransactions.Add(new VendorWalletTransaction
                        {
                            VendorId = vendorId,
                            Amount = amount,
                            Type = "Credit",
                            Reference = refKey,
                            CreatedAt = now,

                            // ✅ LE CHAMP QUI MANQUE POUR “SEMAINE”
                            PayoutBatchId = batch.Id
                        });
                    }

                    // ===============================
                    // ✅ Commission Admin (Ranita)
                    // ===============================
                    var fee = it.CommissionAmount ?? it.PlatformFee;
                    if (fee > 0)
                    {
                        var exists = await db.AdminWalletTransactions
                            .AnyAsync(t => t.Type == "Commission" && t.OrderItemId == it.Id, ct);

                        if (!exists)
                        {
                            db.AdminWalletTransactions.Add(new AdminWalletTransaction
                            {
                                CreatedAt = now,
                                Amount = fee,
                                Type = "Commission",
                                OrderId = it.OrderId,
                                OrderItemId = it.Id,
                                Note = $"COMMISSION:ORDERITEM:{it.Id}"
                            });
                        }
                    }
                    // ✅ Sale détaillée (historique batch)
                    db.VendorPayoutSales.Add(new VendorPayoutSale
                    {
                        VendorPayoutId = payout.Id,
                        OrderId = it.OrderId,
                        OrderItemId = it.Id,
                        ProductId = it.ProductId,
                        ProductName = it.ProductName ?? "",
                        Qty = it.Quantity,
                        UnitPrice = it.UnitPriceSnapshot,
                        LineTotal = it.UnitPriceSnapshot * it.Quantity,
                        PlatformFee = it.PlatformFee,
                        VendorAmount = amount,
                        Amount = amount,
                        SoldAt = it.Order != null ? it.Order.CreatedAt : now,
                        DeliveredAt = it.DeliveredAt,
                        CreatedAt = now
                    });
                    payout.Amount += amount;
                }
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}