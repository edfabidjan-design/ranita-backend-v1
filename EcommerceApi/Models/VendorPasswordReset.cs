namespace EcommerceApi.Models;

public class VendorPasswordReset
{
    public int Id { get; set; }
    public int VendorId { get; set; }
    public string Email { get; set; } = "";
    public string ResetCode { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; }

    public Vendor? Vendor { get; set; }
}