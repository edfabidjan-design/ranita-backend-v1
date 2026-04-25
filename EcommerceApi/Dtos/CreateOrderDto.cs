namespace EcommerceApi.Dtos;

public class CreateOrderDto
{
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string? Note { get; set; }
    public decimal DeliveryFee { get; set; } = 0;

    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class CreateOrderItemDto
{
    public int ProductId { get; set; }
    public int Qty { get; set; }

    // ✅ nouveau
    public int? VariantId { get; set; }

    // ✅ garde-les temporairement pour compatibilité
    public string? Color { get; set; }
    public string? Size { get; set; }
}