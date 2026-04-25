namespace EcommerceApi.Models;

public class VendorPayoutBatch
{
    public int Id { get; set; }

    public DateTime PeriodStart { get; set; }   // du
    public DateTime PeriodEnd { get; set; }     // au (inclus ou exclus selon toi)

    public string Status { get; set; } = "Pending"; // Pending / Processing / Paid / Failed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }

    public string? Provider { get; set; }       // Manual / MoneyFusion / ...
    public string? ProviderRef { get; set; }    // référence virement global





    public decimal TotalAmount { get; set; }
    public int TotalVendors { get; set; }

 
    public string? Note { get; set; }

    public List<VendorPayout> Payouts { get; set; } = new();
}

