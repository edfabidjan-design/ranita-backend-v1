namespace EcommerceApi.Models;

public class ProductAttributeOption
{
    public int Id { get; set; }

    public int ProductAttributeId { get; set; }   // on mappe en SQL sur AttributeId
    public string Value { get; set; } = "";
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public ProductAttribute? Attribute { get; set; }
}
