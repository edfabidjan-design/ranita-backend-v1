namespace EcommerceApi.Models;

public class ReturnItem
{
    public int Id { get; set; }

    public int ReturnRequestId { get; set; }
    public ReturnRequest ReturnRequest { get; set; } = null!;

    public int OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = null!;

    public int ProductId { get; set; }
    public int VendorId { get; set; }
    // ✅ AJOUTE ÇA (quantité retournée)
    public int Quantity { get; set; }
    public int QtyRequested { get; set; }
    public int QtyApproved { get; set; }
    public int QtyReceived { get; set; }

    // Snapshots basés sur OrderItem
    public decimal UnitPriceSnapshot { get; set; }
    public decimal VendorAmountSnapshot { get; set; }
    public decimal PlatformFeeSnapshot { get; set; }
    public decimal CommissionRateSnapshot { get; set; }
    public decimal CommissionAmountSnapshot { get; set; }
    public decimal VendorNetAmountSnapshot { get; set; }

    // Calcul au remboursement
    public decimal RefundLineAmount { get; set; }

    public string? ConditionReceived { get; set; }
    public string? RestockAction { get; set; }
}