using EcommerceApi.Data;
using EcommerceApi.Dtos;
using EcommerceApi.Models;
using EcommerceApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/admin/orders")]

[Authorize]
public class AdminOrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly EmailService _email;
    private readonly IConfiguration _cfg;

    public AdminOrdersController(AppDbContext db, EmailService email, IConfiguration cfg)
    {
        _db = db;
        _email = email;
        _cfg = cfg;
    }




    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "EnAttente",
        "EnPreparation",
        "EnLivraison",
        "Livree",
        "Annulee"
    };

    private static int StatusRank(string s) => s switch
    {
        "EnAttente" => 1,
        "EnPreparation" => 2,
        "EnLivraison" => 3,
        "Livree" => 4,
        "Annulee" => 99,
        _ => 0
    };

    private static string NormalizeStatus(string s) => (s ?? "").Trim();


    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!await HasPermission("orders.view"))
            return Forbid();

        Console.WriteLine("GETALL ADMIN ORDERS HIT");

        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
                .ThenInclude(i => i.Vendor)
            .OrderByDescending(o => o.Id)
            .ToListAsync();

        string VendorSummary(List<OrderItem> items)
        {
            if (items == null || items.Count == 0) return "—";

            int total = items.Count;
            int shipped = items.Count(x => x.VendorStatus == "Shipped");
            int delivered = items.Count(x => x.VendorStatus == "Delivered");

            if (delivered == total) return "Delivered";
            if (shipped == total) return "Shipped";
            if (shipped > 0) return $"Partiel ({shipped}/{total} Shipped)";
            return "Pending";
        }

        string ShopNames(List<OrderItem> items)
        {
            if (items == null || items.Count == 0) return "—";

            var shops = items
                .Where(i => i.VendorId > 0) // 🔥 IMPORTANT
                .Select(i => i.Vendor?.ShopName ?? i.Vendor?.Name)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return shops.Count == 0 ? "Ranita Shop" : string.Join(", ", shops);
        }

        var itemsDto = orders.Select(o => new
        {
            id = o.Id,
            fullName = o.FullName,
            phone = o.Phone,
            city = o.City,
            total = o.Total,
            status = o.Status,
            createdAt = o.CreatedAt,

            vendorStatus = VendorSummary(o.Items),
            vendorShopName = ShopNames(o.Items),

            vendorId = o.Items
    .Where(i => i.VendorId > 0)
    .Select(i => (int?)i.VendorId)
    .FirstOrDefault()
        }).ToList();

        return Ok(new { ok = true, items = itemsDto });
    }    // ✅ 2) DETAIL (avec items)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!await HasPermission("orders.view"))
            return Forbid();

        var order = await _db.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound(new { ok = false, message = "Commande introuvable." });

        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();

        var imagesMap = await _db.ProductImages
            .Where(pi => productIds.Contains(pi.ProductId))
            .OrderByDescending(pi => pi.IsMain)
            .ThenByDescending(pi => pi.Id)
            .GroupBy(pi => pi.ProductId)
            .Select(g => new { ProductId = g.Key, Url = g.First().Url })
            .ToDictionaryAsync(x => x.ProductId, x => x.Url);

        var dto = new AdminOrderDetailDto
        {
            Id = order.Id,
            FullName = order.FullName,
            Phone = order.Phone,
            Email = order.Email,
            City = order.City,
            Address = order.Address,
            Note = order.Note,
            DeliveryFee = order.DeliveryFee,
            SubTotal = order.SubTotal,
            Total = order.Total,
            Status = order.Status,
            CreatedAt = order.CreatedAt,

            Items = order.Items.Select(i => new AdminOrderItemDto
            {
                ProductId = i.ProductId,
                Name = i.ProductName,
                Price = i.UnitPrice,
                Qty = i.Quantity,
                LineTotal = i.UnitPrice * i.Quantity,
                Color = i.SelectedColor,
                Size = i.SelectedSize,

                Dimensions = i.DimensionsSnapshot,
                WeightKg = i.WeightKgSnapshot,

                ImageUrl = imagesMap.TryGetValue(i.ProductId, out var url) ? url : null,

                VendorStatus = i.VendorStatus,      // ✅ AJOUT
                VendorId = i.VendorId               // ✅ AJOUT
            }).ToList()

        };

        return Ok(new { ok = true, item = dto });
    }

    // ✅ 3) UPDATE STATUT
    // ✅ 3) UPDATE STATUT
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        if (!await HasPermission("orders.edit"))
            return Forbid();

        var newStatus = NormalizeStatus(dto?.Status);
        if (string.IsNullOrWhiteSpace(newStatus))
            return BadRequest(new { ok = false, message = "Statut manquant." });

        if (!AllowedStatuses.Contains(newStatus))
            return BadRequest(new
            {
                ok = false,
                message = "Statut invalide. Valeurs: EnAttente, EnPreparation, EnLivraison, Livree, Annulee"
            });

        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound(new { ok = false, message = "Commande introuvable." });

        var current = NormalizeStatus(order.Status);

        if (string.Equals(current, "Livree", StringComparison.OrdinalIgnoreCase)
            && string.Equals(newStatus, "Annulee", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { ok = false, message = "Impossible d'annuler une commande déjà livrée." });

        if (!string.Equals(newStatus, "Annulee", StringComparison.OrdinalIgnoreCase))
        {
            var rCur = StatusRank(current);
            var rNew = StatusRank(newStatus);
            if (rCur != 0 && rNew < rCur)
                return BadRequest(new { ok = false, message = "Transition invalide (retour arrière interdit)." });
        }


        if (string.Equals(newStatus, "Livree", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(current, "EnLivraison", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    ok = false,
                    message = "Impossible de passer à Livree : la commande doit d'abord être en EnLivraison."
                });
            }

            var notShippedByVendor = order.Items.Any(i => !IsVendorShippingReady(i.VendorStatus));

            if (notShippedByVendor)
            {
                return BadRequest(new
                {
                    ok = false,
                    message = "Impossible de passer la commande à Livree : le vendor doit d'abord mettre le produit en Shipping."
                });
            }
        }


        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            var isCancel = string.Equals(newStatus, "Annulee", StringComparison.OrdinalIgnoreCase);
            var wasCancel = string.Equals(current, "Annulee", StringComparison.OrdinalIgnoreCase);

            // ✅ annulation => restore stock 1 fois
            if (isCancel && !wasCancel && !order.StockRestored)
            {
                foreach (var it in order.Items)
                {
                    // ✅ CAS VARIANTE
                    if (it.VariantId.HasValue)
                    {
                        var variant = await _db.ProductVariants
                            .FirstOrDefaultAsync(v => v.Id == it.VariantId.Value);

                        if (variant != null)
                        {
                            variant.Stock += it.Quantity;
                            variant.UpdatedAt = DateTime.UtcNow;
                        }

                        // 🔥 recalcul stock produit = somme variantes
                        var product = await _db.Products
                            .Include(p => p.Variants)
                            .FirstOrDefaultAsync(p => p.Id == it.ProductId);

                        if (product != null)
                        {
                            product.Stock = product.Variants.Sum(v => v.Stock);
                            product.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                    // ✅ CAS PRODUIT SIMPLE
                    else
                    {
                        var p = await _db.Products.FirstOrDefaultAsync(p => p.Id == it.ProductId);
                        if (p != null)
                        {
                            p.Stock += it.Quantity;
                            p.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                order.StockRestored = true;
            }

            // ✅ réactivation => redéduire stock
            if (!isCancel && wasCancel && order.StockRestored)
            {
                foreach (var it in order.Items)
                {
                    if (it.VariantId.HasValue)
                    {
                        var variant = await _db.ProductVariants
                            .FirstOrDefaultAsync(v => v.Id == it.VariantId.Value);

                        if (variant == null)
                            return BadRequest(new { ok = false, message = "Variante introuvable" });

                        if (variant.Stock < it.Quantity)
                            return BadRequest(new { ok = false, message = "Stock insuffisant" });

                        variant.Stock -= it.Quantity;
                        variant.UpdatedAt = DateTime.UtcNow;

                        var product = await _db.Products
                            .Include(p => p.Variants)
                            .FirstOrDefaultAsync(p => p.Id == it.ProductId);

                        if (product != null)
                        {
                            product.Stock = product.Variants.Sum(v => v.Stock);
                            product.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        var p = await _db.Products.FirstOrDefaultAsync(p => p.Id == it.ProductId);

                        if (p == null)
                            return BadRequest(new { ok = false, message = "Produit introuvable" });

                        if (p.Stock < it.Quantity)
                            return BadRequest(new { ok = false, message = "Stock insuffisant" });

                        p.Stock -= it.Quantity;
                        p.UpdatedAt = DateTime.UtcNow;
                    }
                }

                order.StockRestored = false;
            }


            // ✅ statut global
            order.Status = newStatus;

            if (string.Equals(newStatus, "EnLivraison", StringComparison.OrdinalIgnoreCase))
            {
                var notReady = order.Items.Any(i => !IsVendorShippingReady(i.VendorStatus));

                if (notReady)
                    return BadRequest(new
                    {
                        ok = false,
                        message = "Impossible : certains vendeurs n'ont pas encore expédié."
                    });
            }




            // ✅ Livree => DeliveredAt + VendorPayableAt + TRACE (sans doublons)
            if (string.Equals(newStatus, "Livree", StringComparison.OrdinalIgnoreCase))
            {
                order.DeliveredAt ??= DateTime.UtcNow;
                var now = DateTime.UtcNow;
                var adminName = User?.Identity?.Name ?? "admin";

                // ✅ précharger les refs existantes (anti-doublon)
                var refs = order.Items.Select(x => $"OrderItem:{x.Id}").ToList();

                var existingRefs = await _db.VendorWalletTransactions
                    .Where(t => refs.Contains(t.Reference))
                    .Select(t => t.Reference)
                    .ToListAsync();
                var set = existingRefs.ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var it in order.Items)
                {
                    // ✅ IMPORTANT: même si déjà Delivered, on s’assure que la tx existe
                    if (!string.Equals(it.VendorStatus, "Delivered", StringComparison.OrdinalIgnoreCase))
                    {
                        it.VendorStatus = "Delivered";
                        it.DeliveredAt = now;
                        it.VendorPayableAt = now.AddSeconds(120); // ou AddHours(48)
                        it.DeliveredBy = adminName;

                        it.IsVendorPaid = false;
                        it.VendorPaidAt = null;
                    }

                    // ✅ WALLET: Gains générés (Delivered) 1 fois par OrderItem
                    var refKey = $"OrderItem:{it.Id}";
                    if (!set.Contains(refKey))
                    {
                        var amount = it.VendorNetAmount ?? it.VendorAmount;

                        _db.VendorWalletTransactions.Add(new VendorWalletTransaction
                        {
                            VendorId = it.VendorId,
                            Amount = amount,
                            Type = VendorWalletTxTypes.DeliveredCredit, // ou "Delivered" si tu veux garder
                            Reference = refKey,
                            CreatedAt = it.DeliveredAt ?? now,

                            OrderId = it.OrderId,
                            OrderItemId = it.Id,
                            ReturnRequestId = null,

                            PayoutBatchId = null,
                            SettledAt = null
                        });
                        set.Add(refKey);
                    }

                    // 🔥 ICI TU AJOUTES LA COMMISSION ADMIN

                    var commissionRef = $"COMMISSION:ORDERITEM:{it.Id}";

                    var commissionExists = await _db.AdminWalletTransactions
                        .AnyAsync(t => t.Type == "Commission" && t.Note == commissionRef);

                    if (!commissionExists)
                    {
                        _db.AdminWalletTransactions.Add(new AdminWalletTransaction
                        {
                            Type = "Commission",
                            Amount = it.CommissionAmount ?? 0m,
                            OrderId = it.OrderId,
                            OrderItemId = it.Id,
                            CreatedAt = it.DeliveredAt ?? now,
                            Note = commissionRef
                        });
                    }
                }
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        var baseUrl = _cfg["App:BaseUrl"];

        // ✅ Emails APRES commit
        if (!string.IsNullOrWhiteSpace(order.Email))
        {
            var (subject, body) = BuildClientStatusMail(
                order.FullName, order.Id, order.Status,
                order.Items.Select(i => new ReviewMailItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName
                }).ToList(),
                baseUrl
            );

            await _email.SendAsync(order.Email, subject, body);
        }

        var boutiqueEmail = _email.GetBoutiqueEmail();
        if (!string.IsNullOrWhiteSpace(boutiqueEmail))
        {
            await _email.SendAsync(boutiqueEmail,
                $"📌 Ranita - Statut commande #{order.Id} => {order.Status}",
                $"Commande #{order.Id}\nClient: {order.FullName}\nNouveau statut: {order.Status}\nVille: {order.City}\nTotal: {order.Total} FCFA");
        }

        return Ok(new { ok = true, message = "Statut mis à jour.", status = order.Status });
    }
  


    private class ReviewMailItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
    }

    private static (string subject, string body) BuildClientStatusMail(
     string fullName, int orderId, string status,
     List<ReviewMailItem>? items,
     string baseUrl
 )
    {
        string subject = $"📦 Ranita - Mise à jour de votre commande #{orderId}";
        string body;

        switch (status)
        {
            case "Livree":
                var reviewBlocks = string.Join("", (items ?? new List<ReviewMailItem>())
                    .Select(i =>
                    {
                        var link = $"{baseUrl}/product.html?id={i.ProductId}&review=1";

                        return $@"
<div style='margin:14px 0;padding:14px;border:1px solid #e5e7eb;border-radius:12px;background:#f8fafc'>
    <div style='font-weight:700;font-size:15px;color:#111827;margin-bottom:8px'>
        {System.Net.WebUtility.HtmlEncode(i.ProductName)}
    </div>

    <a href='{link}'
       style='display:inline-block;padding:12px 18px;background:#22c55e;color:#06240f;
              text-decoration:none;border-radius:10px;font-weight:800'>
        ⭐ Donner mon avis
    </a>
</div>";
                    }));

                body = $@"
<!doctype html>
<html lang='fr'>
<body style='margin:0;padding:0;background:#f3f4f6;font-family:Arial,sans-serif;color:#111827'>
    <div style='max-width:640px;margin:0 auto;padding:24px'>
        <div style='background:#ffffff;border-radius:16px;padding:24px;border:1px solid #e5e7eb'>
            <h2 style='margin:0 0 12px;color:#111827'>🎉 Commande livrée</h2>

            <p>Bonjour <strong>{System.Net.WebUtility.HtmlEncode(fullName)}</strong>,</p>

            <p>Votre commande <strong>#{orderId}</strong> est marquée comme <strong>LIVRÉE</strong>.</p>

            <p>Merci pour votre confiance. Vous pouvez maintenant donner votre avis sur les produits reçus :</p>

            {reviewBlocks}

            <p style='margin-top:20px'>Merci,<br><strong>Ranita</strong></p>
        </div>
    </div>
</body>
</html>";
                break;

            case "EnPreparation":
                body = $@"
<!doctype html>
<html lang='fr'>
<body style='font-family:Arial,sans-serif'>
    <p>Bonjour <strong>{System.Net.WebUtility.HtmlEncode(fullName)}</strong>,</p>
    <p>✅ Votre commande <strong>#{orderId}</strong> est maintenant <strong>EN PRÉPARATION</strong>.</p>
    <p>Merci,<br><strong>Ranita</strong></p>
</body>
</html>";
                break;

            case "EnLivraison":
                body = $@"
<!doctype html>
<html lang='fr'>
<body style='font-family:Arial,sans-serif'>
    <p>Bonjour <strong>{System.Net.WebUtility.HtmlEncode(fullName)}</strong>,</p>
    <p>🚚 Votre commande <strong>#{orderId}</strong> est maintenant <strong>EN LIVRAISON</strong>.</p>
    <p>Merci,<br><strong>Ranita</strong></p>
</body>
</html>";
                break;

            case "Annulee":
                body = $@"
<!doctype html>
<html lang='fr'>
<body style='font-family:Arial,sans-serif'>
    <p>Bonjour <strong>{System.Net.WebUtility.HtmlEncode(fullName)}</strong>,</p>
    <p>❌ Votre commande <strong>#{orderId}</strong> a été <strong>ANNULÉE</strong>.</p>
    <p>Ranita</p>
</body>
</html>";
                break;

            default:
                body = $@"
<!doctype html>
<html lang='fr'>
<body style='font-family:Arial,sans-serif'>
    <p>Bonjour <strong>{System.Net.WebUtility.HtmlEncode(fullName)}</strong>,</p>
    <p>🕒 Votre commande <strong>#{orderId}</strong> est en attente de traitement.</p>
    <p>Ranita</p>
</body>
</html>";
                break;
        }

        return (subject, body);
    }

    private async Task<bool> HasPermission(string code)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
            return false;

        var user = await _db.Users
            .Include(u => u.RoleRef)!
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        if (user == null || user.RoleRef == null)
            return false;

        if (string.Equals(user.RoleRef.Code, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase))
            return true;

        return user.RoleRef.RolePermissions
            .Any(rp => rp.Permission.Code == code);
    }

    private static bool IsVendorShippingReady(string? status)
    {
        var s = (status ?? "").Trim();

        return string.Equals(s, "Shipped", StringComparison.OrdinalIgnoreCase)
            || string.Equals(s, "Shipping", StringComparison.OrdinalIgnoreCase)
            || string.Equals(s, "EnLivraison", StringComparison.OrdinalIgnoreCase)
            || string.Equals(s, "Delivered", StringComparison.OrdinalIgnoreCase);
    }

}


