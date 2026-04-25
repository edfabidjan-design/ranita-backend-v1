namespace EcommerceApi.Models;

public class VariantValue
{
    public int Id { get; set; }

    public int AxisId { get; set; }
    public VariantAxis Axis { get; set; } = null!;

    // ex: "M", "Noir", "128GB"
    public string Value { get; set; } = "";

    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
