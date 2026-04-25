namespace EcommerceApi.Dtos
{
    public record ProductDetailDto(
     int Id,
     string Name,
     string Slug,
     string? ShortDescription,
     string? Description,
     decimal Price,
     int Stock,
     bool IsActive,
     int? CategoryId,
     string? CategoryName,
     List<ProductImageDto> Images
 );
    public record ProductImageDto(int Id, string Url, bool IsMain, int SortOrder);
}

