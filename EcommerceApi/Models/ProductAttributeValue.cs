using EcommerceApi.Models;

public class ProductAttributeValue
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    // ✅ au lieu de ProductAttributeId
    public int AttributeId { get; set; }
    public ProductAttribute Attribute { get; set; } = null!;

    public int? OptionId { get; set; }
    public ProductAttributeOption? Option { get; set; }

    public string? ValueText { get; set; }
    public int? ValueInt { get; set; }
    public decimal? ValueDecimal { get; set; }
    public bool? ValueBool { get; set; }
    public DateTime? ValueDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int AttributeDefId { get; set; }
  


}
