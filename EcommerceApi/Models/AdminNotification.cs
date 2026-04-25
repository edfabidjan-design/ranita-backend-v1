namespace EcommerceApi.Models;

public class AdminNotification
{
    public int Id { get; set; }

    public string Type { get; set; } = "";        // "ReturnRequested"
    public int? RefId { get; set; }               // ReturnRequestId
    public string Message { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";

    public bool IsRead { get; set; } = false;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}