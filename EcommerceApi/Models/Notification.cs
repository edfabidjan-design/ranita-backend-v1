namespace EcommerceApi.Models;

public class Notification
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string Title { get; set; } = "";
    public string Message { get; set; } = "";

    public string Type { get; set; } = "Return"; // Return / Order / etc.
    public int? RefId { get; set; } // ReturnRequestId par exemple

    public bool IsRead { get; set; } = false;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}