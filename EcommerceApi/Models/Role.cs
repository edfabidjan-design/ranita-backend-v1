namespace EcommerceApi.Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}