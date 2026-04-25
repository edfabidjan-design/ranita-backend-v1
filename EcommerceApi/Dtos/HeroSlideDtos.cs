using System;
using Microsoft.AspNetCore.Http;

namespace EcommerceApi.Dtos;

public class HeroSlideCreateDto
{
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public string? BadgeText { get; set; }
    public string? SmallTag { get; set; }
    public string? PrimaryButtonText { get; set; }
    public string? PrimaryButtonUrl { get; set; }
    public string? SecondaryButtonText { get; set; }
    public string? SecondaryButtonUrl { get; set; }
    public IFormFile? Image { get; set; }
    public string? Theme { get; set; }
    public string? AccentColor { get; set; }
    public string? HighlightText { get; set; }

    public string? RightBadgeText { get; set; }
    public string? RightTitle { get; set; }
    public string? RightSubtitle { get; set; }
    public string? RightButtonText { get; set; }
    public string? RightButtonUrl { get; set; }

    public string? StatsTitle { get; set; }
    public string? Stat1Value { get; set; }
    public string? Stat1Label { get; set; }
    public string? Stat2Value { get; set; }
    public string? Stat2Label { get; set; }
    public string? Stat3Value { get; set; }
    public string? Stat3Label { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class HeroSlideUpdateDto
{
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public string? BadgeText { get; set; }
    public string? SmallTag { get; set; }
    public string? PrimaryButtonText { get; set; }
    public string? PrimaryButtonUrl { get; set; }
    public string? SecondaryButtonText { get; set; }
    public string? SecondaryButtonUrl { get; set; }
    public IFormFile? Image { get; set; }
    public string? Theme { get; set; }
    public string? AccentColor { get; set; }
    public string? HighlightText { get; set; }

    public string? RightBadgeText { get; set; }
    public string? RightTitle { get; set; }
    public string? RightSubtitle { get; set; }
    public string? RightButtonText { get; set; }
    public string? RightButtonUrl { get; set; }

    public string? StatsTitle { get; set; }
    public string? Stat1Value { get; set; }
    public string? Stat1Label { get; set; }
    public string? Stat2Value { get; set; }
    public string? Stat2Label { get; set; }
    public string? Stat3Value { get; set; }
    public string? Stat3Label { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class HeroSlideDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public string? BadgeText { get; set; }
    public string? SmallTag { get; set; }
    public string? PrimaryButtonText { get; set; }
    public string? PrimaryButtonUrl { get; set; }
    public string? SecondaryButtonText { get; set; }
    public string? SecondaryButtonUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? Theme { get; set; }
    public string? AccentColor { get; set; }
    public string? HighlightText { get; set; }

    public string? RightBadgeText { get; set; }
    public string? RightTitle { get; set; }
    public string? RightSubtitle { get; set; }
    public string? RightButtonText { get; set; }
    public string? RightButtonUrl { get; set; }

    public string? StatsTitle { get; set; }
    public string? Stat1Value { get; set; }
    public string? Stat1Label { get; set; }
    public string? Stat2Value { get; set; }
    public string? Stat2Label { get; set; }
    public string? Stat3Value { get; set; }
    public string? Stat3Label { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}