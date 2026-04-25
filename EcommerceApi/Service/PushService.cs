using EcommerceApi.Data;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
public class PushService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;

    public PushService(AppDbContext db, IConfiguration cfg)
    {
        _db = db;
        _cfg = cfg;
    }

    public async Task SendNewOrderToAdminsAsync(int orderId, string city, string fullName, decimal total)
    {
        var tokens = await _db.AdminDeviceTokens
            .Where(t => t.IsActive)
            .Join(_db.Users,
                t => t.UserId,
                u => u.Id,
                (t, u) => new { t.Token, u.Role, u.IsActive })
            .Where(x => x.IsActive &&
                (x.Role == "Admin" || x.Role == "SuperAdmin" || x.Role == "Manager"))
            .Select(x => x.Token)
            .Distinct()
            .ToListAsync();

        Console.WriteLine($"📌 Tokens trouvés: {tokens.Count}");
        if (tokens.Count == 0) return;

        var baseUrl = (_cfg["App:PublicBaseUrl"] ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Console.WriteLine("❌ App:PublicBaseUrl manquant (URL publique HTTPS).");
            return;
        }

        var title = "🛒 Nouvelle commande Ranita";
        var body = $"{fullName} • {city} • {total:n0} FCFA";

        var link = $"{baseUrl}/admin-order.html?id={orderId}";
        var icon = $"{baseUrl}/assets/logo-192.png";

        var msg = new MulticastMessage
        {
            Tokens = tokens,

            Notification = new Notification
            {
                Title = title,
                Body = body
            },

            Data = new Dictionary<string, string>
            {
                ["type"] = "new_order",
                  
                ["orderId"] = orderId.ToString(),
                ["title"] = title,
                ["body"] = body,
                ["link"] = link,
                ["icon"] = icon
            },

            Webpush = new WebpushConfig
            {
                Headers = new Dictionary<string, string>
                {
                    ["TTL"] = "300",
                    ["Urgency"] = "high"
                },
                FcmOptions = new WebpushFcmOptions { Link = link }
            },

            Android = new AndroidConfig
            {
                Priority = Priority.High,
                Notification = new AndroidNotification
                {
                    Sound = "default"
                }
            }
        };

        var res = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(msg);

        for (int i = 0; i < res.Responses.Count; i++)
        {
            Console.WriteLine($"➡️ result[{i}] success={res.Responses[i].IsSuccess}");
            if (!res.Responses[i].IsSuccess)
                Console.WriteLine("❌ error=" + res.Responses[i].Exception?.Message);
        }


        Console.WriteLine($"📨 FCM => Success={res.SuccessCount}, Failure={res.FailureCount}");
        for (int i = 0; i < res.Responses.Count; i++)
            if (!res.Responses[i].IsSuccess)
                Console.WriteLine("❌ FCM error: " + res.Responses[i].Exception?.Message);
    }

    public async Task SendNewReturnToAdminsAsync(int returnId, int orderId, string customerName)
    {
        var tokens = await _db.AdminDeviceTokens
            .Where(t => t.IsActive)
            .Join(_db.Users,
                t => t.UserId,
                u => u.Id,
                (t, u) => new { t.Token, u.Role, u.IsActive })
            .Where(x => x.IsActive &&
                (x.Role == "Admin" || x.Role == "SuperAdmin" || x.Role == "Manager"))
            .Select(x => x.Token)
            .Distinct()
            .ToListAsync();

        Console.WriteLine($"📌 Tokens admin (retour) trouvés: {tokens.Count}");
        if (tokens.Count == 0) return;

        var baseUrl = (_cfg["App:PublicBaseUrl"] ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Console.WriteLine("❌ App:PublicBaseUrl manquant (URL publique HTTPS).");
            return;
        }

        var title = "↩️ Nouvelle demande de retour";
        var body = $"{customerName} • Commande #{orderId} • Retour #{returnId}";
        var link = $"{baseUrl}/admin-returns.html?id={returnId}";
        var icon = $"{baseUrl}/assets/logo-192.png";

        var msg = new MulticastMessage
        {
            Tokens = tokens,

            // ✅ DATA ONLY
            Data = new Dictionary<string, string>
            {
                ["type"] = "new_return",
                ["returnId"] = returnId.ToString(),
                ["orderId"] = orderId.ToString(),
                ["title"] = title,
                ["body"] = body,
                ["link"] = link,
                ["icon"] = icon
            },

            Webpush = new WebpushConfig
            {
                Headers = new Dictionary<string, string>
                {
                    ["TTL"] = "300",
                    ["Urgency"] = "high"
                },
                FcmOptions = new WebpushFcmOptions { Link = link }
            }
        };

        var res = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(msg);
        Console.WriteLine($"📨 FCM Return => Success={res.SuccessCount}, Failure={res.FailureCount}");

        for (int i = 0; i < res.Responses.Count; i++)
            if (!res.Responses[i].IsSuccess)
                Console.WriteLine("❌ FCM error: " + res.Responses[i].Exception?.Message);
    }


    public async Task SendNewVendorProductToAdminsAsync(int productId, string productName, string vendorName)
    {
        var tokens = await _db.AdminDeviceTokens
            .Where(t => t.IsActive)
            .Join(_db.Users,
                t => t.UserId,
                u => u.Id,
                (t, u) => new { t.Token, u.Role, u.IsActive })
            .Where(x => x.IsActive &&
                (x.Role == "Admin" || x.Role == "SuperAdmin" || x.Role == "Manager" || x.Role == "CatalogManager"))
            .Select(x => x.Token)
            .Distinct()
            .ToListAsync();

        Console.WriteLine($"📌 Tokens admin (vendor product) trouvés: {tokens.Count}");
        if (tokens.Count == 0) return;

        var baseUrl = (_cfg["App:PublicBaseUrl"] ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Console.WriteLine("❌ App:PublicBaseUrl manquant.");
            return;
        }

        var title = "📦 Nouveau produit à valider";
        var body = $"{productName} • Boutique {vendorName}";
        var link = $"{baseUrl}/admin-product-detail.html?id={productId}";
        var icon = $"{baseUrl}/assets/logo-192.png";

        var msg = new MulticastMessage
        {
            Tokens = tokens,

            // ✅ DATA ONLY
            Data = new Dictionary<string, string>
            {
                ["type"] = "vendor_product",
                ["productId"] = productId.ToString(),
                ["vendorName"] = vendorName ?? "",
                ["title"] = title,
                ["body"] = body,
                ["link"] = link,
                ["icon"] = icon
            },

            Webpush = new WebpushConfig
            {
                Headers = new Dictionary<string, string>
                {
                    ["TTL"] = "300",
                    ["Urgency"] = "high"
                },
                FcmOptions = new WebpushFcmOptions
                {
                    Link = link
                }
            },

            Android = new AndroidConfig
            {
                Priority = Priority.High
            }
        };

        var res = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(msg);
        Console.WriteLine($"📨 FCM VendorProduct => Success={res.SuccessCount}, Failure={res.FailureCount}");

        for (int i = 0; i < res.Responses.Count; i++)
        {
            if (!res.Responses[i].IsSuccess)
                Console.WriteLine("❌ FCM error: " + res.Responses[i].Exception?.Message);
        }
    }

    public async Task SendNewVendorRequestToAdminsAsync(int vendorId, string shopName, string ownerName)
    {
        var tokens = await _db.AdminDeviceTokens
            .Where(t => t.IsActive)
            .Join(_db.Users,
                t => t.UserId,
                u => u.Id,
                (t, u) => new { t.Token, u.Role, u.IsActive })
            .Where(x => x.IsActive &&
                (x.Role == "Admin" || x.Role == "SuperAdmin" || x.Role == "Manager" || x.Role == "CatalogManager"))
            .Select(x => x.Token)
            .Distinct()
            .ToListAsync();

        Console.WriteLine($"📌 Tokens admin (vendor request) trouvés: {tokens.Count}");
        if (tokens.Count == 0) return;

        var baseUrl = (_cfg["App:PublicBaseUrl"] ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Console.WriteLine("❌ App:PublicBaseUrl manquant.");
            return;
        }

        var title = "🏪 Nouvelle demande de boutique";
        var body = $"{shopName} • {ownerName}";
        var link = $"{baseUrl}/admin-vendors.html?id={vendorId}";
        var icon = $"{baseUrl}/assets/logo-192.png";

        var msg = new MulticastMessage
        {
            Tokens = tokens,

            Data = new Dictionary<string, string>
            {
                ["type"] = "vendor_request",
                ["vendorId"] = vendorId.ToString(),
                ["shopName"] = shopName ?? "",
                ["ownerName"] = ownerName ?? "",
                ["title"] = title,
                ["body"] = body,
                ["link"] = link,
                ["icon"] = icon
            },

            Webpush = new WebpushConfig
            {
                Headers = new Dictionary<string, string>
                {
                    ["TTL"] = "300",
                    ["Urgency"] = "high"
                },
                FcmOptions = new WebpushFcmOptions
                {
                    Link = link
                }
            },

            Android = new AndroidConfig
            {
                Priority = Priority.High
            }
        };

        var res = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(msg);
        Console.WriteLine($"📨 FCM VendorRequest => Success={res.SuccessCount}, Failure={res.FailureCount}");

        for (int i = 0; i < res.Responses.Count; i++)
        {
            if (!res.Responses[i].IsSuccess)
                Console.WriteLine("❌ FCM error: " + res.Responses[i].Exception?.Message);
        }
    }
}