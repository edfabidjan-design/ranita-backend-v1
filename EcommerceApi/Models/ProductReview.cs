namespace EcommerceApi.Models;

public class ProductReview
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int? OrderId { get; set; }
    public Order? Order { get; set; }

    public byte Rating { get; set; } // 1..5
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public bool VerifiedPurchase { get; set; } = false;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}