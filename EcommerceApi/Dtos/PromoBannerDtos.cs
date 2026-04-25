namespace EcommerceApi.Dtos;

public class SavePromoBannerDto
{
    public string Title { get; set; } = "";
    public string? SubTitle { get; set; }
    public string? Description { get; set; }
    public string PromoCode { get; set; } = "";

    public string? PrimaryButtonText { get; set; }
    public string? PrimaryButtonLink { get; set; }

    public string? SecondaryButtonText { get; set; }
    public string? SecondaryButtonLink { get; set; }

    public string? ImageUrl { get; set; }
    public string? MobileImageUrl { get; set; }

    public string? BackgroundColor { get; set; }
    public string? TextColor { get; set; }

    public string SideTitle { get; set; } = "";
    public string SideText { get; set; } = "";

    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }

    public string BackgroundImageUrl { get; set; } = "";
    public string Theme { get; set; } = "promo";
    public string AccentColor { get; set; } = "#22c55e";
    public string Position { get; set; } = "after-hero";
    public int Priority { get; set; } = 1;

    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}