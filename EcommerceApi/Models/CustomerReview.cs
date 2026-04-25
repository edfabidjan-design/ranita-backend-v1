namespace EcommerceApi.Models;

public class CustomerReview
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public string? CustomerRole { get; set; }
    public string ReviewText { get; set; } = "";
    public int Rating { get; set; } = 5;
    public string? AvatarUrl { get; set; }

    public string? ProductName { get; set; }
    public string? ProductLink { get; set; }

    public string? City { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}