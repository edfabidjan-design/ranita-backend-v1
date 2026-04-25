namespace EcommerceApi.Models;

public class VendorDailyStat
{
    public int Id { get; set; }

    public int VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public DateTime Day { get; set; } // on stocke la date (UTC.Date)

    public int DeliveredOrdersCount { get; set; }
    public int DeliveredItemsCount { get; set; }
    public decimal DeliveredRevenue { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}