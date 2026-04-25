using EcommerceApi.Data;
using EcommerceApi.Models;
using EcommerceApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Reflection.Metadata;
using System.Security.Claims;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/admin/vendor-payouts")]
[Authorize]
public class AdminVendorPayoutsController : ControllerBase
{
    private readonly AppDbContext _db;

    private readonly EmailService _email;

    public AdminVendorPayoutsController(AppDbContext db, EmailService email)
    {
        _db = db;
        _email = email;
    }

    // =========================================================
    // 1) LISTE UI (ton code existant)
    // GET /api/admin/vendor-payouts?mode=pending|payable|paid|all
    // =========================================================
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? mode = "all")
    {
        if (!await HasPermission("payouts.view"))
            return Forbid();

        var now = DateTime.UtcNow;
        var nowTs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // ✅ important
        mode = (mode ?? "all").Trim().ToLowerInvariant();

        var q = _db.OrderItems
            .AsNoTracking()
            .Include(i => i.Vendor)
            .Where(i => i.VendorStatus == "Delivered")
            .Where(i => i.DeliveredAt != null)
            .Where(i => !i.IsVendorPaid)
            .Where(i => (i.VendorPayoutId == null || i.VendorPayoutId == 0)) // ✅ pas batché
            .Where(i => (i.Quantity - i.RefundedQuantity) > 0);

        // ✅ filtres onglets
        if (mode == "payable")
            q = q.Where(i => i.VendorPayableAt != null && i.VendorPayableAt <= now);
        else if (mode == "pending")
            q = q.Where(i => i.VendorPayableAt == null || i.VendorPayableAt > now);
        else if (mode == "paid")
        {
            // onglet Paid = ceux déjà payés (même s'ils ne sont plus dans q)
            var paidItems = await _db.OrderItems
                .AsNoTracking()
                .Include(i => i.Vendor)
                .Where(i => i.VendorStatus == "Delivered")
                .Where(i => i.IsVendorPaid)
                .Where(i => (i.Quantity - i.RefundedQuantity) > 0)
                .OrderByDescending(i => i.Id)
                .Select(i => new {
                    id = i.Id,
                    orderId = i.OrderId,
                    vendorId = i.VendorId,
                    vendorName = i.Vendor != null ? i.Vendor.Name : ("Vendor#" + i.VendorId),
                    quantity = i.Quantity,
                    refundedQuantity = i.RefundedQuantity,
                    qtyRemaining = (i.Quantity - i.RefundedQuantity),
                    amount = i.VendorAmount * (i.Quantity - i.RefundedQuantity),
                    deliveredAt = i.DeliveredAt,
                    payableAt = i.VendorPayableAt,
                    isPaid = i.IsVendorPaid,
                    paidAt = i.VendorPaidAt,
                    vendorPayoutId = i.VendorPayoutId
                })
                .ToListAsync();

            return Ok(new { ok = true, now, nowTs, mode, totalPayableNow = 0m, items = paidItems });
        }

        var items = await q.OrderByDescending(i => i.Id)
            .Select(i => new {
                id = i.Id,
                orderId = i.OrderId,
                vendorId = i.VendorId,
                vendorName = i.Vendor != null ? i.Vendor.Name : ("Vendor#" + i.VendorId),

                quantity = i.Quantity,
                refundedQuantity = i.RefundedQuantity,
                qtyRemaining = (i.Quantity - i.RefundedQuantity),

                amount = i.VendorAmount * (i.Quantity - i.RefundedQuantity),

                deliveredAt = i.DeliveredAt,
                payableAt = i.VendorPayableAt,
                isPaid = i.IsVendorPaid,
                paidAt = i.VendorPaidAt,
                vendorPayoutId = i.VendorPayoutId
            })
            .ToListAsync();

        var totalPayableNow =
            await PayableNowQuery(now)
                .SumAsync(i => (decimal?)(i.VendorAmount * (i.Quantity - i.RefundedQuantity))) ?? 0m;

        return Ok(new { ok = true, now, nowTs, mode, totalPayableNow, items });
    }

    // =========================================================
    // 2) LISTE DES BATCHS
    // GET /api/admin/vendor-payouts/batches
    // =========================================================
    [HttpGet("batches")]
    public async Task<IActionResult> GetBatches()
    {
        if (!await HasPermission("payouts.view"))
            return Forbid();

        // 1) batchs (infos de base)
        var baseBatches = await _db.VendorPayoutBatches
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Select(x => new
            {
                id = x.Id,
                periodStart = x.PeriodStart,
                periodEnd = x.PeriodEnd,
                status = x.Status,
                createdAt = x.CreatedAt,
                paidAt = x.PaidAt,
                provider = x.Provider,
                providerRef = x.ProviderRef,

                // ⚠️ vendorsCount = nombre de payouts (normalement 1 payout par vendeur)
                vendorsCount = _db.VendorPayouts.Count(p => p.BatchId == x.Id),
                total = _db.VendorPayouts.Where(p => p.BatchId == x.Id).Sum(p => (decimal?)p.Amount) ?? 0m
            })
            .ToListAsync();

        var batchIds = baseBatches.Select(b => b.id).ToList();
        if (batchIds.Count == 0)
            return Ok(new { ok = true, batches = baseBatches });

        // 2) récupérer les noms de boutiques par batch (1 requête)
        var namesRows = await _db.VendorPayouts
            .AsNoTracking()
            .Where(p => batchIds.Contains(p.BatchId))
            .Include(p => p.Vendor)
            .Select(p => new
            {
                p.BatchId,
                Shop =
                    (p.Vendor != null
                        ? ((p.Vendor.ShopName ?? p.Vendor.Name) ?? ("Vendor#" + p.VendorId))
                        : ("Vendor#" + p.VendorId))
            })
            .Distinct()
            .ToListAsync();

        // 3) construire un preview "A, B +N"
        var previewMap = namesRows
            .GroupBy(x => x.BatchId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var all = g.Select(x => x.Shop).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                    if (all.Count == 0) return "—";
                    var first2 = all.Take(2).ToList();
                    var preview = string.Join(", ", first2);
                    if (all.Count > 2) preview += $" +{all.Count - 2}";
                    return preview;
                });

        // 4) réponse finale
        var batches = baseBatches.Select(b => new
        {
            b.id,
            b.periodStart,
            b.periodEnd,
            b.status,
            b.createdAt,
            b.paidAt,
            b.provider,
            b.providerRef,
            b.vendorsCount,
            b.total,
            shopsPreview = previewMap.TryGetValue(b.id, out var s) ? s : "—"
        });

        return Ok(new { ok = true, batches });
    }

    // =========================================================
    // 3) CREER BATCH (période du..au..)
    // POST /api/admin/vendor-payouts/batches/create
    // body: { "periodStart": "2026-02-23T00:00:00Z", "periodEnd": "2026-03-01T23:59:59Z" }
    // =========================================================
    public record CreateBatchRequest(DateTime PeriodStart, DateTime PeriodEnd);

    [HttpPost("batches/create")]
    public async Task<IActionResult> CreateBatch([FromBody] CreateBatchRequest req)
    {
        if (!await HasPermission("payouts.manage"))
            return Forbid();

        var now = DateTime.UtcNow;

        if (req.PeriodEnd <= req.PeriodStart)
            return BadRequest(new { ok = false, error = "PeriodEnd doit être > PeriodStart." });

        var exists = await _db.VendorPayoutBatches
            .AnyAsync(x => x.PeriodStart == req.PeriodStart && x.PeriodEnd == req.PeriodEnd);

        if (exists)
            return Conflict(new { ok = false, error = "Batch déjà existant pour cette période." });

        var payableItems = await PayableNowQuery(now)
            .ToListAsync();

        if (payableItems.Count == 0)
            return Ok(new { ok = true, created = false, message = "Aucun item payable à inclure." });

        // ✅ Créer batch
        var batch = new VendorPayoutBatch
        {
            PeriodStart = req.PeriodStart,
            PeriodEnd = req.PeriodEnd,
            Status = "Processing",
            CreatedAt = now
        };

        _db.VendorPayoutBatches.Add(batch);
        await _db.SaveChangesAsync(); // batch.Id

        // ✅ UN SEUL groups (montant restant)
        var groups = payableItems
            .GroupBy(i => i.VendorId)
            .Select(g => new
            {
                VendorId = g.Key,
                Amount = g.Sum(x => x.VendorAmount * (x.Quantity - x.RefundedQuantity)),
                Items = g.ToList()
            })
            .ToList();

        foreach (var g in groups)
        {
            var payout = new VendorPayout
            {
                BatchId = batch.Id,
                VendorId = g.VendorId,
                Amount = g.Amount,
                Status = "Pending",
                CreatedAt = now
            };

            _db.VendorPayouts.Add(payout);
            await _db.SaveChangesAsync(); // payout.Id

            foreach (var it in g.Items)
                it.VendorPayoutId = payout.Id;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            ok = true,
            created = true,
            batchId = batch.Id,
            vendors = groups.Count,
            items = payableItems.Count,
            total = groups.Sum(x => x.Amount)
        });
    }
    // =========================================================
    // 4) MARQUER BATCH PAYE (paiement manuel ou provider)
    // POST /api/admin/vendor-payouts/batches/{batchId}/mark-paid
    // body: { "provider": "Manual", "providerRef": "VIR-2026-03-01-001" }
    // =========================================================
    public record MarkPaidRequest(string? Provider, string? ProviderRef);

    [HttpPost("batches/{batchId:int}/mark-paid")]
    public async Task<IActionResult> MarkBatchPaid(int batchId, [FromBody] MarkPaidRequest req)
    {
        if (!await HasPermission("payouts.manage"))
            return Forbid();

        var now = DateTime.UtcNow;

        var batch = await _db.VendorPayoutBatches
            .FirstOrDefaultAsync(x => x.Id == batchId);

        if (batch == null)
            return NotFound(new { ok = false, error = "Batch introuvable." });

        if (batch.Status == "Paid")
            return Ok(new { ok = true, message = "Batch déjà payé." });

        var payouts = await _db.VendorPayouts
            .Where(p => p.BatchId == batchId)
            .ToListAsync();

        if (payouts.Count == 0)
            return BadRequest(new { ok = false, error = "Aucun payout dans ce batch." });

        // ✅ Update batch
        batch.Status = "Paid";
        batch.PaidAt = now;
        batch.Provider = string.IsNullOrWhiteSpace(req.Provider) ? "Manual" : req.Provider.Trim();
        batch.ProviderRef = req.ProviderRef?.Trim();

        // ✅ Update payouts
        foreach (var p in payouts)
        {
            p.Status = "Paid";
            p.PaidAt = now;
        }

        // ✅ Mark order items paid
        // ✅ Mark order items paid (UNIQUEMENT dans la période du batch)
        var payoutIds = payouts.Select(p => p.Id).ToList();

        var items = await _db.OrderItems
            .Where(i => i.VendorPayoutId != null && payoutIds.Contains(i.VendorPayoutId.Value))
            .ToListAsync();

        foreach (var it in items)
        {
            it.IsVendorPaid = true;
            it.VendorPaidAt = now;
        }

        await _db.SaveChangesAsync();


        // ===============================
        // 📩 ENVOI EMAIL AUX VENDEURS (après paiement batch)
        // ===============================

        var vendorIds = payouts.Select(p => p.VendorId).Distinct().ToList();

        // récupère les emails owners + nom boutique (ShopName)
        var vendorContacts = await _db.Vendors
            .Where(v => vendorIds.Contains(v.Id))
            .Select(v => new
            {
                VendorId = v.Id,
                ShopName = v.Name, // ✅ NOM DE LA BOUTIQUE
                Email = _db.VendorUsers
                    .Where(u => u.VendorId == v.Id && u.IsOwner)
                    .Select(u => u.Email)
                    .FirstOrDefault()
            })
            .ToListAsync();

        foreach (var payout in payouts)
        {
            var vc = vendorContacts.FirstOrDefault(x => x.VendorId == payout.VendorId);
            if (vc == null) continue;
            if (string.IsNullOrWhiteSpace(vc.Email)) continue;

            var subject = $"Paiement Ranita effectué (Batch #{batch.Id})";

            var body = $@"
Bonjour {vc.ShopName},

Votre paiement vendeur a été effectué ✅

Batch : #{batch.Id}
Période : {batch.PeriodStart:dd/MM/yyyy} → {batch.PeriodEnd:dd/MM/yyyy}
Montant payé : {payout.Amount:N0} FCFA
Payé le : {now:dd/MM/yyyy HH:mm} (UTC)
Référence : {batch.ProviderRef ?? "—"}

Merci de vendre sur Ranita.
Equipe Ranita
";

            await _email.SendAsync(vc.Email, subject, body);
        }


        return Ok(new
        {
            ok = true,
            batchId,
            vendors = payouts.Count,
            items = items.Count,
            total = payouts.Sum(x => x.Amount)
        });
    }

    // =======================================
    // GET /api/admin/vendor-payouts/batches/{id}
    // Détails d'un batch
    // =======================================
    [HttpGet("batches/{batchId:int}")]
    public async Task<IActionResult> GetBatchDetails(int batchId)
    {
        if (!await HasPermission("payouts.view"))
            return Forbid();

        var batch = await _db.VendorPayoutBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == batchId);

        if (batch == null)
            return NotFound(new { ok = false, error = "Batch introuvable." });

        var payouts = await _db.VendorPayouts
            .AsNoTracking()
            .Include(p => p.Vendor)
            .Where(p => p.BatchId == batchId)
            .Select(p => new
            {
                vendorId = p.VendorId,
                vendorName = p.Vendor.Name,
                amount = p.Amount,
                status = p.Status,
                itemsCount = _db.OrderItems.Count(i => i.VendorPayoutId == p.Id)
            })
            .ToListAsync();

        return Ok(new
        {
            ok = true,
            batch = new
            {
                id = batch.Id,
                periodStart = batch.PeriodStart,
                periodEnd = batch.PeriodEnd,
                status = batch.Status,
                createdAt = batch.CreatedAt,
                paidAt = batch.PaidAt
            },
            payouts
        });
    }


    // ...

    [HttpPost("batches/create-payable-now")]
    public async Task<IActionResult> CreateBatchPayableNow()
    {
        if (!await HasPermission("payouts.manage"))
            return Forbid();

        var now = DateTime.UtcNow;

        // ✅ Items payables maintenant, NON payés, NON batchés
        var items = await PayableNowQuery(now)
            .Select(i => new {
                i.Id,
                i.VendorId,
                Amount = i.VendorAmount * (i.Quantity - i.RefundedQuantity)
            })
            .ToListAsync();

        if (items.Count == 0)
            return Ok(new { ok = true, created = false, message = "Aucun item payable maintenant." });

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {

            var periodStart = now;
            var periodEnd = now;
            // ✅ Période “libre” (juste info)
            var batch = new VendorPayoutBatch
            {
                PeriodStart = now,
                PeriodEnd = now,
                Status = "Pending",
                CreatedAt = now,
                Provider = "Manual",
                ProviderRef = null,
                PaidAt = null
            };

            _db.VendorPayoutBatches.Add(batch);
            await _db.SaveChangesAsync(); // batch.Id

            // ✅ Payouts par vendeur
            var payouts = items
                .GroupBy(x => x.VendorId)
                .Select(g => new VendorPayout
                {
                    BatchId = batch.Id,
                    VendorId = g.Key,
                    Amount = g.Sum(x => x.Amount),
                    Status = "Pending",
                    CreatedAt = now
                })
                .ToList();

            _db.VendorPayouts.AddRange(payouts);
            await _db.SaveChangesAsync(); // payout.Id

            var map = payouts.ToDictionary(p => p.VendorId, p => p.Id);
            var itemIds = items.Select(x => x.Id).ToList();

            // ✅ rattacher items au payout
            var orderItemsToUpdate = await _db.OrderItems
                .Where(i => itemIds.Contains(i.Id))
                .ToListAsync();

            foreach (var oi in orderItemsToUpdate)
                oi.VendorPayoutId = map[oi.VendorId];

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new
            {
                ok = true,
                created = true,
                batchId = batch.Id,
                vendors = payouts.Count,
                items = items.Count,
                total = items.Sum(x => x.Amount)
            });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }



    [HttpGet("batches/{batchId:int}/pdf")]
public async Task<IActionResult> ExportBatchPdf(int batchId)
{
        if (!await HasPermission("payouts.view"))
            return Forbid();

        var batch = await _db.VendorPayoutBatches
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == batchId);

    if (batch == null)
        return NotFound(new { ok = false, error = "Batch introuvable." });

        var payouts = await _db.VendorPayouts
            .AsNoTracking()
            .Include(p => p.Vendor)
            .Where(p => p.BatchId == batchId)
            .Select(p => new
            {
                VendorId = p.VendorId,
                VendorName = p.Vendor != null ? p.Vendor.Name : ("Vendor#" + p.VendorId),

                // ✅ EMAIL depuis VendorUsers (Owner)
                Email = _db.VendorUsers
                    .Where(u => u.VendorId == p.VendorId && u.IsOwner)
                    .Select(u => u.Email)
                    .FirstOrDefault(),

                // ✅ TELEPHONE depuis Vendors (si existe)
                Phone = p.Vendor.Phone,

                Amount = p.Amount,
                ItemsCount = _db.OrderItems.Count(i => i.VendorPayoutId == p.Id)
            })
            .OrderByDescending(x => x.Amount)
            .ToListAsync();

        var total = payouts.Sum(x => x.Amount);
    var now = DateTime.UtcNow;

        var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
        {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(25);
            page.DefaultTextStyle(x => x.FontSize(11));

            page.Header().Column(col =>
            {
                col.Item().Text("RANITA - LISTE DE PAIEMENT VENDEURS").FontSize(16).SemiBold();
                col.Item().Text($"Batch #{batch.Id}  |  Statut: {batch.Status}");
                col.Item().Text($"Période: {batch.PeriodStart:dd/MM/yyyy HH:mm} → {batch.PeriodEnd:dd/MM/yyyy HH:mm}");
                col.Item().Text($"Généré le: {now:dd/MM/yyyy HH:mm} (UTC)");
                col.Item().LineHorizontal(1);
            });

            page.Content().Column(col =>
            {
                col.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);   // ID
                        columns.RelativeColumn(3);    // Vendeur
                        columns.RelativeColumn(2);    // Téléphone ✅
                        columns.RelativeColumn(3);    // Email ✅
                        columns.ConstantColumn(60);   // Items
                        columns.RelativeColumn(2);    // Montant
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("ID").SemiBold();
                        header.Cell().Text("Vendeur").SemiBold();
                        header.Cell().Text("Téléphone").SemiBold(); // ✅
                        header.Cell().Text("Email").SemiBold();     // ✅
                        header.Cell().AlignRight().Text("Items").SemiBold();
                        header.Cell().AlignRight().Text("Montant").SemiBold();

                        static IContainer CellStyle(IContainer c) =>
                            c.PaddingVertical(6).PaddingHorizontal(4).Background(Colors.Grey.Lighten3);
                    });

                    foreach (var p in payouts)
                    {
                        table.Cell().Text(p.VendorId.ToString());
                        table.Cell().Text(p.VendorName);

                        table.Cell().Text(string.IsNullOrWhiteSpace(p.Phone) ? "—" : p.Phone); // ✅
                        table.Cell().Text(string.IsNullOrWhiteSpace(p.Email) ? "—" : p.Email); // ✅

                        table.Cell().AlignRight().Text(p.ItemsCount.ToString());
                        table.Cell().AlignRight().Text($"{p.Amount:N0} FCFA");

                        static IContainer CellRow(IContainer c) =>
                            c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(6).PaddingHorizontal(4);
                    }
                });

                col.Item().PaddingTop(12).AlignRight().Text($"TOTAL: {total:N0} FCFA").FontSize(13).SemiBold();
            });

            page.Footer().AlignRight().Text("Document interne - Ranita");
        });
    }).GeneratePdf();

    var filename = $"Batch_{batch.Id}_PaiementsVendeurs_{now:yyyyMMdd_HHmm}.pdf";
    return File(pdfBytes, "application/pdf", filename);
}

    private IQueryable<OrderItem> PayableNowQuery(DateTime nowUtc)
    {
        return _db.OrderItems
            .Where(i => i.VendorStatus == "Delivered")
            .Where(i => i.DeliveredAt != null)
            .Where(i => !i.IsVendorPaid)
            .Where(i => (i.VendorPayoutId == null || i.VendorPayoutId == 0)) // ✅ pas déjà batché
            .Where(i => (i.Quantity - i.RefundedQuantity) > 0)
            .Where(i => i.VendorPayableAt != null && i.VendorPayableAt <= nowUtc); // ✅ payable “maintenant”
    }

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
}