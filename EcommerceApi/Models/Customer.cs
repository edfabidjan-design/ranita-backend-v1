namespace EcommerceApi.Models;

public class Customer
{
    public int Id { get; set; }

    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";          // unique
    public string? Email { get; set; }

    public string PasswordHash { get; set; } = "";

    public bool IsActive { get; set; } = true;
  
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
