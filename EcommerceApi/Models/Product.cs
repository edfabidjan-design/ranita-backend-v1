namespace EcommerceApi.Models;

public class Product
{
    public int Id { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }
    public int Stock { get; set; }

    public bool IsActive { get; set; } = true;
    
    public string? ShortDescription { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<ProductImage> Images { get; set; } = new();

    public string? Brand { get; set; }
    public string? Sku { get; set; }

    public decimal? PricePromo { get; set; }   // promo optionnel


    public string? Sizes { get; set; }   // ✅
    public string? Colors { get; set; }  // ✅

    public string? LongDescription { get; set; }
    public string? Highlights { get; set; }

    public decimal? WeightKg { get; set; }
    public string? Dimensions { get; set; }

    public decimal RatingAvg { get; set; } = 0m;
    public int RatingCount { get; set; } = 0;
    public int? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public string PublishedStatus { get; set; } = "Pending"; // Pending/Published/Rejected
    public bool IsDeleted { get; set; } = false;

    public string? RejectReason { get; set; }
    public DateTime? RejectedAt { get; set; }


    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}
