namespace EcommerceApi.Dtos;

public class CreateUserDto
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public int? RoleId { get; set; }
}