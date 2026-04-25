namespace EcommerceApi.Models;

public class AdminDeviceToken
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Token { get; set; } = "";
    public string? Platform { get; set; }
    public string? DeviceName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}