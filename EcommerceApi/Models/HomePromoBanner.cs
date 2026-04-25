namespace EcommerceApi.Models
{
    public class HomePromoBanner
    {
        public int Id { get; set; }

        public string? Title { get; set; }
        public string? Subtitle { get; set; }

        public string? PromoCode { get; set; }

        public string? PrimaryButtonText { get; set; }
        public string? PrimaryButtonUrl { get; set; }

        public string? SecondaryButtonText { get; set; }
        public string? SecondaryButtonUrl { get; set; }

        public string? SideTitle { get; set; }
        public string? SideText { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}