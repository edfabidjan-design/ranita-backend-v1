namespace EcommerceApi.Dtos
{
    public class VendorCreateProductDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = "";
        public string Sku { get; set; } = "";
        public decimal Price { get; set; }
        public decimal? PricePromo { get; set; }
        public int Stock { get; set; }
        public string? Brand { get; set; }

        // ✅ aligné admin
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string? LongDescription { get; set; }
        public List<string>? Highlights { get; set; }

        public decimal? WeightKg { get; set; }
        public string? Dimensions { get; set; }
    }
}
