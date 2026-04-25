using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceApi.Models
{
    public class HomeEventCampaign
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = "";

        [MaxLength(300)]
        public string? Subtitle { get; set; }

        [MaxLength(80)]
        public string? BadgeText { get; set; }

        [MaxLength(500)]
        public string? DesktopImageUrl { get; set; }

        [MaxLength(500)]
        public string? MobileImageUrl { get; set; }

        [MaxLength(80)]
        public string? ButtonText { get; set; }

        [MaxLength(500)]
        public string? ButtonLink { get; set; }

        [Required]
        [MaxLength(30)]
        public string TargetType { get; set; } = "url";
        // url / category / products

        public int? CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }

        [MaxLength(30)]
        public string? BackgroundColor { get; set; }

        [MaxLength(30)]
        public string? TextColor { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public bool IsFeatured { get; set; } = false;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}