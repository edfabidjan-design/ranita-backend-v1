using System.ComponentModel.DataAnnotations.Schema;

[Table("VendorAccounts")]
public class VendorAccount
{
    public int Id { get; set; }
    public int VendorId { get; set; }

    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public decimal WalletBalance { get; set; } = 0m;
    public string? WalletName { get; set; }
    public Vendor? Vendor { get; set; }
}