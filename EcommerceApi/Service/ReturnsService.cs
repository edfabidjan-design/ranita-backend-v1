using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

public class ReturnsService
{
    private readonly AppDbContext _db;
    public ReturnsService(AppDbContext db) => _db = db;

    public async Task RefundAsync(int returnId, int adminUserId, string refundRef, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var rr = await _db.Set<ReturnRequest>()
            .Include(r => r.Items)
                .ThenInclude(i => i.OrderItem)
            .FirstOrDefaultAsync(r => r.Id == returnId, ct);

        if (rr == null) throw new Exception("Retour introuvable.");
        if (rr.Status != "Received" && rr.Status != "Approved")
            throw new Exception("Le retour doit être Approved ou Received avant remboursement.");

        decimal totalRefund = 0m;

        foreach (var it in rr.Items)
        {
            var oi = it.OrderItem;

            var qty = it.QtyReceived > 0 ? it.QtyReceived : it.QtyApproved;
            if (qty <= 0) throw new Exception("Quantité reçue/validée invalide.");

            var remaining = oi.Quantity - oi.RefundedQuantity;
            if (qty > remaining) throw new Exception("Quantité retour > quantité restante.");

            // prorata
            var ratio = (decimal)qty / (decimal)oi.Quantity;

            var refundLine = oi.UnitPriceSnapshot * qty;
            it.RefundLineAmount = refundLine;
            totalRefund += refundLine;

            // impact vendeur (débit)
            var vendorDebit = (oi.VendorNetAmount ?? oi.VendorAmount) * ratio;

            _db.Set<VendorWalletTransaction>().Add(new VendorWalletTransaction
            {
                VendorId = oi.VendorId,
                Amount = -vendorDebit,
                Type = "RefundDebit",
                Reference = $"RETURN#{rr.Id}-ITEM#{oi.Id}",
                OrderId = oi.OrderId,
                OrderItemId = oi.Id,
                ReturnRequestId = rr.Id,
                CreatedAt = DateTime.UtcNow
            });

            // maj order item
            oi.RefundedQuantity += qty;
            oi.ReturnStatus = "Refunded";
        }

        rr.RefundAmount = totalRefund;
        rr.RefundMethod = "Cash";
        rr.RefundReference = refundRef;
        rr.AdminUserId = adminUserId;
        rr.Status = "Refunded";
        rr.RefundedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}