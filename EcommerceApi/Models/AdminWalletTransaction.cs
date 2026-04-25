using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceApi.Models;

public class AdminWalletTransaction
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public string Type { get; set; } = "Commission"; // Commission, Adjustment...

    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    public int? OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }
    public string Note { get; set; } = "";
}