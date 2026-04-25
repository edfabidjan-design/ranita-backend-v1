namespace EcommerceApi.Dtos;

public class ReplaceVariantsRequest
{
    public string? VariantMode { get; set; }
    public List<VariantStockDto> Items { get; set; } = new();
}
