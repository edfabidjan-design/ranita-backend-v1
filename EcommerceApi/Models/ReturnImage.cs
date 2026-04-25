namespace EcommerceApi.Models;

public class ReturnImage
{
    public int Id { get; set; }

    public int ReturnRequestId { get; set; }
    public ReturnRequest ReturnRequest { get; set; } = null!;

    public string ImageUrl { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}