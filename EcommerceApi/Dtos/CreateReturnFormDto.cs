namespace EcommerceApi.Dtos
{
    public class CreateReturnFormDto
    {
        public int OrderId { get; set; }
        public string? Reason { get; set; }
        public string? Comment { get; set; }
        public string? ItemsJson { get; set; }

        // ✅ correspond à fd.append("images", file)
        public List<IFormFile>? Images { get; set; }
    }
}
