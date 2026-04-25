namespace EcommerceApi.Dtos;

public class ProductListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Slug { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public string? MainImageUrl { get; set; }

    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public decimal? PricePromo { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Dimensions { get; set; }
    public string PublishedStatus { get; set; } = "";
    public int? VendorId { get; set; }

}
