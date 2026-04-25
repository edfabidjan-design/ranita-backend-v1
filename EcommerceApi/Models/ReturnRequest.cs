namespace EcommerceApi.Models;

public class ReturnRequest
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string Status { get; set; } = "Requested";
    // Requested / Approved / Rejected / Received / Refunded / Closed

    public string Reason { get; set; } = "";
    public string? CustomerComment { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public int? AdminUserId { get; set; }
    public string? AdminNote { get; set; }
    // ✅ AJOUTE ÇA (le texte libre du client)
    public string? Comment { get; set; }

    // ✅ AJOUTE ÇA (date création)
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public decimal RefundAmount { get; set; }

    public string RefundMethod { get; set; } = "Cash";
    public string? RefundReference { get; set; }
    public List<ReturnImage> Images { get; set; } = new();
    public List<ReturnItem> Items { get; set; } = new();

}