namespace EcommerceApi.Dtos.Admin;

public class AdminDashboardOverviewDto
{
    public DashboardKpisDto Kpis { get; set; } = new();
    public List<DashboardSeriesPointDto> SalesByDay { get; set; } = new();
    public List<DashboardSeriesPointDto> CommissionsByDay { get; set; } = new();
    public List<DashboardRecentOrderDto> RecentOrders { get; set; } = new();
    public List<DashboardNotificationDto> Notifications { get; set; } = new();
    public List<DashboardAlertDto> Alerts { get; set; } = new();
}

public class DashboardKpisDto
{
    public int OrdersToday { get; set; }
    public int OrdersMonth { get; set; }

    public decimal RevenueToday { get; set; }
    public decimal RevenueMonth { get; set; }

    public decimal CommissionToday { get; set; }
    public decimal CommissionMonth { get; set; }

    public int PendingOrders { get; set; }
    public int ReturnsToProcess { get; set; }

    public int ActiveProducts { get; set; }
    public int ActiveVendors { get; set; }
}

public class DashboardSeriesPointDto
{
    public string Label { get; set; } = "";
    public decimal Value { get; set; }
}

public class DashboardRecentOrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public decimal Total { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class DashboardNotificationDto
{
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string? Link { get; set; }
}

public class DashboardAlertDto
{
    public string Type { get; set; } = "";
    public string Label { get; set; } = "";
    public string Severity { get; set; } = "info";
    public string? Link { get; set; }
}