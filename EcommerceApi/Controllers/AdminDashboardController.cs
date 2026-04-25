using EcommerceApi.Data;
using EcommerceApi.Dtos.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize]
public class AdminDashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminDashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<AdminDashboardOverviewDto>> GetOverview()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var tomorrow = today.AddDays(1);

        var monthStart = new DateTime(now.Year, now.Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        var validRevenueStatuses = new[] { "EnPreparation", "EnLivraison", "Livree" };

        var ordersQuery = _db.Orders.AsNoTracking();
        var orderItemsQuery = _db.OrderItems.AsNoTracking();

        var ordersToday = await ordersQuery
            .CountAsync(o => o.CreatedAt >= today && o.CreatedAt < tomorrow);

        var ordersMonth = await ordersQuery
            .CountAsync(o => o.CreatedAt >= monthStart && o.CreatedAt < nextMonth);

        var pendingOrders = await ordersQuery
            .CountAsync(o => o.Status == "EnAttente");

        var revenueToday = await ordersQuery
            .Where(o => o.CreatedAt >= today &&
                        o.CreatedAt < tomorrow &&
                        validRevenueStatuses.Contains(o.Status))
            .SumAsync(o => (decimal?)o.Total) ?? 0m;

        var revenueMonth = await ordersQuery
            .Where(o => o.CreatedAt >= monthStart &&
                        o.CreatedAt < nextMonth &&
                        validRevenueStatuses.Contains(o.Status))
            .SumAsync(o => (decimal?)o.Total) ?? 0m;

        // ✅ Ici on suppose que PlatformFee existe dans OrderItems.
        // Si chez toi le nom est différent, remplace juste PlatformFee par le bon champ.
        var walletTxQuery = _db.AdminWalletTransactions.AsNoTracking();

        var commissionToday = await walletTxQuery
            .Where(x => x.Type != null &&
                        x.Type.Contains("Commission") &&
                        x.CreatedAt >= today &&
                        x.CreatedAt < tomorrow)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        var commissionMonth = await walletTxQuery
            .Where(x => x.Type != null &&
                        x.Type.Contains("Commission") &&
                        x.CreatedAt >= monthStart &&
                        x.CreatedAt < nextMonth)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        var activeProducts = await _db.Products.AsNoTracking()
            .CountAsync(p => p.IsActive);

        var activeVendors = await _db.Vendors.AsNoTracking()
            .CountAsync();

        int returnsToProcess = 0;
        try
        {
            returnsToProcess = await _db.ReturnRequests.AsNoTracking()
                .CountAsync(r => r.Status == "Requested");
        }
        catch
        {
            returnsToProcess = 0;
        }

        var recentOrders = await ordersQuery
            .OrderByDescending(o => o.CreatedAt)
            .Take(8)
            .Select(o => new DashboardRecentOrderDto
            {
                Id = o.Id,
                OrderNumber = "CMD-" + o.Id,
                CustomerName = o.FullName ?? "Client",
                Total = o.Total,
                Status = o.Status,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        var salesByDay = new List<DashboardSeriesPointDto>();
        var commissionsByDay = new List<DashboardSeriesPointDto>();

        for (int i = 6; i >= 0; i--)
        {
            var dayStart = today.AddDays(-i);
            var dayEnd = dayStart.AddDays(1);

            var dayRevenue = await ordersQuery
                .Where(o => o.CreatedAt >= dayStart &&
                            o.CreatedAt < dayEnd &&
                            validRevenueStatuses.Contains(o.Status))
                .SumAsync(o => (decimal?)o.Total) ?? 0m;

            var dayCommission = await walletTxQuery
                .Where(x => x.Type != null &&
                            x.Type.Contains("Commission") &&
                            x.CreatedAt >= dayStart &&
                            x.CreatedAt < dayEnd)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            salesByDay.Add(new DashboardSeriesPointDto
            {
                Label = dayStart.ToString("dd/MM"),
                Value = dayRevenue
            });

            commissionsByDay.Add(new DashboardSeriesPointDto
            {
                Label = dayStart.ToString("dd/MM"),
                Value = dayCommission
            });
        }

        var notifications = new List<DashboardNotificationDto>();

        foreach (var o in recentOrders.Take(5))
        {
            notifications.Add(new DashboardNotificationDto
            {
                Type = "order",
                Title = "Nouvelle activité commande",
                Message = $"Commande {o.OrderNumber} - {o.Status}",
                CreatedAt = o.CreatedAt,
                Link = "/admin-orders.html"
            });
        }

        try
        {
            var latestReturns = await _db.ReturnRequests.AsNoTracking()
                .OrderByDescending(r => r.RequestedAt)
                .Take(5)
                .ToListAsync();

            foreach (var r in latestReturns)
            {
                notifications.Add(new DashboardNotificationDto
                {
                    Type = "return",
                    Title = "Demande de retour",
                    Message = $"Retour demandé pour commande #{r.OrderId}",
                    CreatedAt = r.RequestedAt,
                    Link = "/admin-returns.html"
                });
            }
        }
        catch
        {
        }

        var alerts = new List<DashboardAlertDto>();

        try
        {
            var lowStockCount = await _db.Products.AsNoTracking()
                .CountAsync(p => p.IsActive && !p.IsDeleted && p.Stock <= 5);

            if (lowStockCount > 0)
            {
                alerts.Add(new DashboardAlertDto
                {
                    Type = "stock",
                    Label = $"{lowStockCount} produit(s) en stock faible",
                    Severity = "warning",
                    Link = "/admin-products-moderation.html?stock=low&status=Published"
                });
            }
        }
        catch
        {
        }

        if (pendingOrders > 0)
        {
            alerts.Add(new DashboardAlertDto
            {
                Type = "orders",
                Label = $"{pendingOrders} commande(s) en attente",
                Severity = "info",
                Link = "/admin-orders.html"
            });
        }

        if (returnsToProcess > 0)
        {
            alerts.Add(new DashboardAlertDto
            {
                Type = "returns",
                Label = $"{returnsToProcess} retour(s) à traiter",
                Severity = "danger",
                Link = "/admin-returns.html"
            });
        }

        var dto = new AdminDashboardOverviewDto
        {
            Kpis = new DashboardKpisDto
            {
                OrdersToday = ordersToday,
                OrdersMonth = ordersMonth,
                RevenueToday = revenueToday,
                RevenueMonth = revenueMonth,
                CommissionToday = commissionToday,
                CommissionMonth = commissionMonth,
                PendingOrders = pendingOrders,
                ReturnsToProcess = returnsToProcess,
                ActiveProducts = activeProducts,
                ActiveVendors = activeVendors
            },
            SalesByDay = salesByDay,
            CommissionsByDay = commissionsByDay,
            RecentOrders = recentOrders,
            Notifications = notifications
                .OrderByDescending(x => x.CreatedAt)
                .Take(8)
                .ToList(),
            Alerts = alerts
        };

        return Ok(dto);
    }

    [HttpGet("top-products")]
    public async Task<IActionResult> TopProducts()
    {
        var data = await _db.OrderItems
            .GroupBy(x => new { x.ProductId, x.ProductName })
            .Select(g => new
            {
                productId = g.Key.ProductId,
                name = g.Key.ProductName,
                quantity = g.Sum(x => x.Quantity),
                revenue = g.Sum(x => x.UnitPrice * x.Quantity)
            })
            .OrderByDescending(x => x.quantity)
            .Take(10)
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("top-vendors")]
    public async Task<IActionResult> GetTopVendors()
    {
        var data = await _db.OrderItems
            .Where(i => i.Order.Status == "Livree")
            .GroupBy(i => new { i.VendorId, i.Vendor.Name })
            .Select(g => new
            {
                name = g.Key.Name,
                revenue = g.Sum(x => x.UnitPrice * x.Quantity)
            })
            .OrderByDescending(x => x.revenue)
            .Take(10)
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("sales-by-city")]
    public async Task<IActionResult> SalesByCity()
    {
        static string NormalizeCity(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";

            var s = value.Trim().ToLowerInvariant();

            s = s
                .Replace("é", "e")
                .Replace("è", "e")
                .Replace("ê", "e")
                .Replace("ë", "e")
                .Replace("à", "a")
                .Replace("â", "a")
                .Replace("ä", "a")
                .Replace("î", "i")
                .Replace("ï", "i")
                .Replace("ô", "o")
                .Replace("ö", "o")
                .Replace("ù", "u")
                .Replace("û", "u")
                .Replace("ü", "u")
                .Replace("ç", "c")
                .Replace("-", " ");

            while (s.Contains("  "))
                s = s.Replace("  ", " ");

            return s;
        }

        var citiesRaw = await _db.Cities
            .AsNoTracking()
            .Where(c => c.Latitude != null && c.Longitude != null)
            .ToListAsync();

        var cityRefs = citiesRaw
            .Select(c => new
            {
                c.Name,
                Normalized = NormalizeCity(c.Name),
                Latitude = c.Latitude,
                Longitude = c.Longitude,
                Level = "City",
                CityName = c.Name
            })
            .ToList();

        var districtRaw = await _db.Districts
            .AsNoTracking()
            .Join(
                _db.Cities.AsNoTracking(),
                d => d.CityId,
                c => c.Id,
                (d, c) => new
                {
                    Name = d.Name,
                    CityName = c.Name,
                    d.Latitude,
                    d.Longitude
                })
            .Where(x => x.Latitude != null && x.Longitude != null)
            .ToListAsync();

        var districtRefs = districtRaw
            .Select(x => new
            {
                x.Name,
                Normalized = NormalizeCity(x.Name),
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                Level = "District",
                x.CityName
            })
            .ToList();

        var refs = new List<dynamic>();
        refs.AddRange(cityRefs);
        refs.AddRange(districtRefs);

        var salesRaw = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Status == "Livree" && o.City != null && o.City != "")
            .Select(o => new
            {
                o.City,
                o.Total
            })
            .ToListAsync();

        var sales = salesRaw
            .Select(o => new
            {
                City = o.City!,
                Normalized = NormalizeCity(o.City),
                o.Total
            })
            .ToList();

        var data = sales
            .GroupBy(x => x.Normalized)
            .Select(g =>
            {
                var locationRef = refs.FirstOrDefault(r => r.Normalized == g.Key);

                return new
                {
                    city = locationRef?.Name ?? g.First().City,
                    sales = g.Sum(x => x.Total),
                    ordersCount = g.Count(),
                    latitude = locationRef?.Latitude,
                    longitude = locationRef?.Longitude,
                    level = locationRef?.Level ?? "Unknown"
                };
            })
            .Where(x => x.latitude != null && x.longitude != null)
            .OrderByDescending(x => x.sales)
            .ToList();

        return Ok(data);
    }
}