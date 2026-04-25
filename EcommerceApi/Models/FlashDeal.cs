namespace EcommerceApi.Models
{
    public class FlashDeal
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public decimal DiscountPercent { get; set; }

        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }

        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}