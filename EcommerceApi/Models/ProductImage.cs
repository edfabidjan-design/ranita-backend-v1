namespace EcommerceApi.Models;

public class ProductImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public string FileName { get; set; } = "";   // nom fichier sur disque
    public string Url { get; set; } = "";        // URL publique (/uploads/....)
    public bool IsMain { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int SortOrder { get; set; } = 0;

}

