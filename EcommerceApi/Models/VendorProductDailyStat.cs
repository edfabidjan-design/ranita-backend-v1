namespace EcommerceApi.Models;

public class VendorProductDailyStat
{
    public int Id { get; set; }

    public int VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public DateTime Day { get; set; }

    public int QtySold { get; set; }
    public decimal Revenue { get; set; }
}