using System;

namespace EcommerceApi.Models.Dtos
{
    public class HomeEventCampaignCreateUpdateDto
    {
        public string Title { get; set; } = "";
        public string? Subtitle { get; set; }
        public string? BadgeText { get; set; }

        public string? DesktopImageUrl { get; set; }
        public string? MobileImageUrl { get; set; }

        public string? ButtonText { get; set; }
        public string? ButtonLink { get; set; }

        public string TargetType { get; set; } = "url";
        public int? CategoryId { get; set; }

        public string? BackgroundColor { get; set; }
        public string? TextColor { get; set; }

        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;

        public bool IsSeasonal { get; set; }
        public string? SeasonKey { get; set; }
        public string? AutoScheduleType { get; set; }
        public int? MonthStart { get; set; }
        public int? DayStart { get; set; }
        public int? MonthEnd { get; set; }
        public int? DayEnd { get; set; }
        public int Priority { get; set; }
        public bool AutoActivate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}