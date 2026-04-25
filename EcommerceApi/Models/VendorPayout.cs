namespace EcommerceApi.Models;

public class VendorPayout
{
    public int Id { get; set; }

    public int BatchId { get; set; }
    public VendorPayoutBatch Batch { get; set; } = null!;

    public int VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending"; // Pending / Paid / Failed

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }

    public string? PaymentRef { get; set; }
    public string? FailureReason { get; set; }

    public string? ProviderRef { get; set; }    // ref du virement du vendeur
    public List<VendorPayoutSale> Sales { get; set; } = new();
}