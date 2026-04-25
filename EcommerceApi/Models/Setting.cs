namespace EcommerceApi.Models;

public class Setting
{
    public int Id { get; set; }      // si la table l’a
    public string Key { get; set; } = "";
    public string? Value { get; set; }
    public DateTime UpdatedAt { get; set; }
}