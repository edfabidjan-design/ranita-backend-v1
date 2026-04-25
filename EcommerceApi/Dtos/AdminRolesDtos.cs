namespace EcommerceApi.Dtos;

public class RoleCreateDto
{
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? Description { get; set; }
}

public class RoleUpdateDto
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

public class UpdateRolePermissionsDto
{
    public List<int> PermissionIds { get; set; } = new();
}