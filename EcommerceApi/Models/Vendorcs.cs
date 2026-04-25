using EcommerceApi.Models;

public class Vendor
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";

    // 0=EnAttente, 1=Actif, 2=Bloqué
    public int Status { get; set; } = 0;
    public string? ContractPdfPath { get; set; }
    public decimal CommissionRate { get; set; } = 0m;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ✅ 1 compte par vendeur
    public VendorAccount? Account { get; set; }
    public string ShopName { get; set; } = "";
    public List<VendorUser> VendorUsers { get; set; } = new();
    public ICollection<Product> Products { get; set; } = new List<Product>();

    public bool TermsEmailSent { get; set; } = false;
    public DateTime? TermsEmailSentAt { get; set; }
    public string? SignedContractPath { get; set; }
    public DateTime? SignedContractReceivedAt { get; set; }

    public bool SignedAgreementReceived { get; set; } = false;
    public DateTime? SignedAgreementReceivedAt { get; set; }
    public DateTime? WalletUpdatedAt { get; set; }    // optionnel (suivi)
 
}
