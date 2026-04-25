namespace EcommerceApi.Models;

public class CategoryAttribute
{
    public int CategoryId { get; set; }
    public int AttributeId { get; set; }

    public bool IsRequired { get; set; } = false;
    public bool IsFilterable { get; set; } = false;
    public int SortOrder { get; set; } = 0;

    // ✅ Copie pratique (remplie côté serveur depuis Attributes.IsVariant)
    public bool IsVariant { get; set; } = false;

    // Navigations
    public Category? Category { get; set; }
    public ProductAttribute? Attribute { get; set; }
}
