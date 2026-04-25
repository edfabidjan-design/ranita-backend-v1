namespace EcommerceApi.Models;

public class ProductVariant
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Stock { get; set; }
    public decimal? PriceOverride { get; set; } // optionnel
    public string? Size { get; set; }   // ✅
    public string? Color { get; set; }  // ✅


    // ✅ valeurs lisibles (ex: "42", "Noir")
    public string Key1 { get; set; } = "Unique";
    public string Key2 { get; set; } = "Unique";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<ProductVariantValue> Values { get; set; } = new List<ProductVariantValue>();
}
