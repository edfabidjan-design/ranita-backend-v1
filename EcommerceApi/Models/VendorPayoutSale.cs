namespace EcommerceApi.Models;

public class VendorPayoutSale
{
    public int Id { get; set; }

    // ✅ relation unique vers VendorPayout (Batch/Payout)
    public int VendorPayoutId { get; set; }
    public VendorPayout VendorPayout { get; set; } = null!;

    // ✅ lien vers l'OrderItem (recommandé)
    public int OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = null!;

    // Snapshots utiles (tu peux garder)
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public decimal PlatformFee { get; set; }
    public decimal VendorAmount { get; set; }

    public DateTime SoldAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    // (optionnel) pour ton worker : montant credité dans ce payout
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}