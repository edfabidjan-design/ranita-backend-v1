namespace EcommerceApi.Dtos
{
    public class ClientResetPasswordDto
    {
        public string Login { get; set; } = "";
        public string Code { get; set; } = "";
        public string NewPassword { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
    }
}