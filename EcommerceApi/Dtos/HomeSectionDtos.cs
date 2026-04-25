namespace EcommerceApi.Dtos;

public class SaveHomeSectionDto
{
    public string SectionKey { get; set; } = "";
    public string? Title { get; set; }
    public string? SubTitle { get; set; }
    public string? Description { get; set; }

    public string? PrimaryButtonText { get; set; }
    public string? PrimaryButtonLink { get; set; }

    public string? SecondaryButtonText { get; set; }
    public string? SecondaryButtonLink { get; set; }

    public string? ImageUrl { get; set; }
    public string? BadgeText { get; set; }

    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public string? BackgroundColor { get; set; }
    public string? TextColor { get; set; }
}