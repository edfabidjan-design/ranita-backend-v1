namespace EcommerceApi.Models;

public class HomeSectionItem
{
    public int Id { get; set; }

    public int HomeSectionId { get; set; }
    public HomeSection? HomeSection { get; set; }

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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}