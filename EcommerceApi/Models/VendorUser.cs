using EcommerceApi.Models;

public class VendorUser
{
    public int Id { get; set; }

    public int VendorId { get; set; }
    public Vendor Vendor { get; set; } = default!;

    public bool IsOwner { get; set; } = true;

    public string Email { get; set; } = "";
    public string Username { get; set; } = "";   // unique

    public string PasswordHash { get; set; } = "";

    public bool IsActive { get; set; } = true;
    public string Role { get; set; } = "Vendor";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
