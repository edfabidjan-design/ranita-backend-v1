namespace EcommerceApi.Models;

public class CategoryCommissionRule
{
    public int Id { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = default!;

    // 0.1500 = 15%
    public decimal CommissionRate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}