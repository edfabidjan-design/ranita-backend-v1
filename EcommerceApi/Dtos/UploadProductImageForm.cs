namespace EcommerceApi.Dtos;

public class UploadProductImageForm
{
    public IFormFile File { get; set; } = default!;
    public bool IsMain { get; set; } = false;
}
