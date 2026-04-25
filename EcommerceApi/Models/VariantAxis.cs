namespace EcommerceApi.Models;

public class VariantAxis
{
    public int Id { get; set; }

    // ex: "size", "color", "storage", "strap"
    public string Key { get; set; } = "";

    // ex: "Taille", "Couleur"
    public string Name { get; set; } = "";

    public bool IsActive { get; set; } = true;

    public List<VariantValue> Values { get; set; } = new();
}
