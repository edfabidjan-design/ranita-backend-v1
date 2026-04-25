namespace EcommerceApi.Dtos;

public class VendorAuthDtos
{
    public record VendorRegisterDto(string VendorName, string Email, string Password, string Phone);
    public record VendorLoginDto(string Email, string Password);
}