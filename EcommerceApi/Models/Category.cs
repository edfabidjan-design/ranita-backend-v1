namespace EcommerceApi.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public List<Category> Children { get; set; } = new();

    public int SortOrder { get; set; } = 0;

  

    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Product> Products { get; set; } = new();
    public List<CategoryAttribute> CategoryAttributes { get; set; } = new();
    public decimal? CommissionRate { get; set; } // null => non défini (hérite)

}
