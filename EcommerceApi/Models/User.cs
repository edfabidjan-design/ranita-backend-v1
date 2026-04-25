using System.Data;

namespace EcommerceApi.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";

    // ✅ ancien champ gardé temporairement pour migration douce
    public string Role { get; set; } = "Admin";

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ✅ nouveau système dynamique
    public int? RoleId { get; set; }
    public Role? RoleRef { get; set; }
}