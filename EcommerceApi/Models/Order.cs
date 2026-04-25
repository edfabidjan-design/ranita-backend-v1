namespace EcommerceApi.Models;

public class Order
{
    public int Id { get; set; }

    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string? Note { get; set; }

    public decimal DeliveryFee { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Total { get; set; }

    public string Status { get; set; } = "EnAttente";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<OrderItem> Items { get; set; } = new();
    public bool StockRestored { get; set; } = false;

    // ✅ Lien compte client (optionnel)
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public bool IsPaid { get; set; } = false;
    public DateTime? PaidAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    // optionnel (utile pour reporting rapide)
    public decimal AdminCommissionTotal { get; set; } = 0m;
    public string? PaymentRef { get; set; }

}

