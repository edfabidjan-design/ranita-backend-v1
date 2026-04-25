namespace EcommerceApi.Dtos;

public class AdminOrderListDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string City { get; set; } = "";
    public decimal Total { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class AdminOrderDetailDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";

    public string City { get; set; } = "";
    public string Address { get; set; } = "";
    public string? Note { get; set; }

    public decimal DeliveryFee { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Total { get; set; }

    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public List<AdminOrderItemDto> Items { get; set; } = new();
}

public class AdminOrderItemDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Qty { get; set; }
    public decimal LineTotal { get; set; }

    public string? Color { get; set; }
    public string? Size { get; set; }

    public string VendorStatus { get; set; } = "";
    public int VendorId { get; set; }
    public string? Dimensions { get; set; }
    public decimal? WeightKg { get; set; }
    public string? ImageUrl { get; set; }
}

public class UpdateOrderStatusDto
{
    public string Status { get; set; } = "";
}
