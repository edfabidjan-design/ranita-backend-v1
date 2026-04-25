namespace EcommerceApi.Models;

public class ProductVariantValue
{
    public int Id { get; set; }

    public int ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;

    public int AxisId { get; set; }
    public VariantAxis Axis { get; set; } = null!;

    public int ValueId { get; set; }
    public VariantValue Value { get; set; } = null!;
}
