namespace EcommerceApi.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int ProductId { get; set; } // pour référence technique (optionnel, mais utile)
    public Product? Product { get; set; }   // ✅ navigation

    public int? VariantId { get; set; }
    public ProductVariant? Variant { get; set; }

    public bool IsSeenByVendor { get; set; } = false;

    // ✅ SNAPSHOT AU MOMENT DE L'ACHAT
    public string ProductName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    // ✅ choix client (variant simple pour l’instant)
    public string? SelectedColor { get; set; }
    public string? SelectedSize { get; set; }

    // ✅ logistique snapshot
    public string? SkuSnapshot { get; set; }
    public decimal? WeightKgSnapshot { get; set; }
    public string? DimensionsSnapshot { get; set; }

    // ✅ MARKETPLACE
    public int VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;
    public bool IsVendorPaid { get; set; } = false;
    public int? VendorPayoutId { get; set; }
    public VendorPayout? VendorPayout { get; set; }
    public string? DeliveredBy { get; set; }
    public DateTime? VendorPaidAt { get; set; }
    public decimal UnitPriceSnapshot { get; set; }     // même valeur que UnitPrice
    public decimal VendorAmount { get; set; }          // part vendeur
    public decimal PlatformFee { get; set; }           // commission Ranita
    public DateTime? DeliveredAt { get; set; }          // date livraison (quand VendorStatus passe à Delivered)
    public DateTime? VendorPayableAt { get; set; }      // livraison + 48h / 7j

    public string VendorStatus { get; set; } = "Pending";
    // Pending / Accepted / Shipped / Delivered / Cancelled
    // ✅ Commission figée au paiement (en %)
    public decimal? CommissionRateSnapshot { get; set; }   // ex: 10.0 (=10%)

   public decimal CommissionRate { get; set; }

    // ✅ Montants figés au paiement
    public decimal? CommissionAmount { get; set; }         // même valeur que PlatformFee
    public decimal? VendorNetAmount { get; set; }          // même valeur que VendorAmount


    public int RefundedQuantity { get; set; } = 0; // quantité déjà remboursée
    public string ReturnStatus { get; set; } = "None"; // None/Requested/Approved/Received/Refunded


}
