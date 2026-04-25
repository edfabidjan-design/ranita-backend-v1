namespace EcommerceApi.Models;

public enum AttributeDataType
{
    Text = 0,
    Option = 1
}

public class ProductAttribute
{
    public int Id { get; set; }

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    // ✅ enum (et non string)
    public AttributeDataType DataType { get; set; } = AttributeDataType.Text;

    public bool IsVariant { get; set; } = false;

    // ✅ si ta DB n'a pas la colonne, on la créera (étape 4)
    public bool IsActive { get; set; } = true;

    public ICollection<ProductAttributeOption> Options { get; set; } = new List<ProductAttributeOption>();
}
