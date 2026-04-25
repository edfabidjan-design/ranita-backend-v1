namespace EcommerceApi.Dtos;

public class SaveHomeSectionItemDto
{
    public int HomeSectionId { get; set; }
    public string ItemType { get; set; } = "card";
    public string? Title { get; set; }
    public string? SubTitle { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? IconClass { get; set; }
    public string? ButtonText { get; set; }
    public string? ButtonLink { get; set; }
    public string? BadgeText { get; set; }
    public string? PriceText { get; set; }
    public string? MetaText { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}