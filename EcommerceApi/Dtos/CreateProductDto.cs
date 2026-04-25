namespace EcommerceApi.Dtos;

public class CreateProductDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }

    public int? VendorId { get; set; }
    public string? ShortDescription { get; set; }
  
    public bool IsActive { get; set; } = true;

    public string? Brand { get; set; }
    public string? Sku { get; set; }
    public decimal? PricePromo { get; set; }
    public string? VariantMode { get; set; } = "Simple";
 


    public string? LongDescription { get; set; }
    public string? Highlights { get; set; }

    public decimal? WeightKg { get; set; }
    public string? Dimensions { get; set; }

    public string? Sizes { get; set; }
    public string? Colors { get; set; }

    public decimal? PoidsKg { get; set; }

}

public class UpdateProductDto : CreateProductDto { }
