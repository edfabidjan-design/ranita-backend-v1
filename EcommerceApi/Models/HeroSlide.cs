namespace EcommerceApi.Models;

public class HeroSlide
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

    public string? Theme { get; set; } // promo / new / brand / category
    public string? AccentColor { get; set; } // ex: #22c55e
    public string? HighlightText { get; set; } // mot à mettre en couleur

    // ===== Partie droite =====
    public string? RightBadgeText { get; set; }
    public string? RightTitle { get; set; }
    public string? RightSubtitle { get; set; }
    public string? RightButtonText { get; set; }
    public string? RightButtonUrl { get; set; }

    // ===== Bloc stats =====
    public string? StatsTitle { get; set; }

    public string? Stat1Value { get; set; }
    public string? Stat1Label { get; set; }

    public string? Stat2Value { get; set; }
    public string? Stat2Label { get; set; }

    public string? Stat3Value { get; set; }
    public string? Stat3Label { get; set; }

    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}