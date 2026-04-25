using EcommerceApi.Data;
using EcommerceApi.Dtos;
using EcommerceApi.Models;
using EcommerceApi.Service;
using EcommerceApi.Service.Background;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly EmailService _email;
    private readonly IBackgroundTaskQueue _queue;
    private readonly CommissionResolver _commission;
    private readonly IConfiguration _cfg;
    private readonly PushService _push;

    public OrdersController(
        AppDbContext db,
        EmailService email,
        IBackgroundTaskQueue queue,
        CommissionResolver commission,
        IConfiguration cfg,
        PushService push)
    {
        _db = db;
        _email = email;
        _queue = queue;
        _commission = commission;
        _cfg = cfg;
        _push = push;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            if (dto == null || dto.Items == null || dto.Items.Count == 0)
                return BadRequest(new { ok = false, message = "Panier vide." });

            if (string.IsNullOrWhiteSpace(dto.FullName) ||
                string.IsNullOrWhiteSpace(dto.Phone) ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Address) ||
                string.IsNullOrWhiteSpace(dto.City))
            {
                return BadRequest(new { ok = false, message = "Informations livraison manquantes." });
            }

            var ids = dto.Items.Select(i => i.ProductId).Distinct().ToList();

            var products = await _db.Products
                .Include(p => p.Variants)
                .Include(p => p.Vendor)
                .Where(p => ids.Contains(p.Id) && p.IsActive)
                .ToListAsync();

            if (products.Count != ids.Count)
                return BadRequest(new { ok = false, message = "Un produit du panier n'existe plus." });

            decimal subTotal = 0m;
            var orderItems = new List<OrderItem>();

            foreach (var it in dto.Items)
            {
                if (it.Qty <= 0)
                    return BadRequest(new { ok = false, message = "Quantité invalide." });

                var p = products.FirstOrDefault(x => x.Id == it.ProductId);
                if (p == null)
                    return BadRequest(new { ok = false, message = $"Produit introuvable: {it.ProductId}" });

                var unit = EffectivePrice(p);
                var lineTotal = unit * it.Qty;
                subTotal += lineTotal;

                var fallbackVendorId = await _db.Vendors
                    .AsNoTracking()
                    .OrderBy(v => v.Id)
                    .Select(v => v.Id)
                    .FirstOrDefaultAsync();

                if (fallbackVendorId <= 0)
                {
                    return StatusCode(500, new
                    {
                        ok = false,
                        message = "Aucun vendeur trouvé en base. Crée au moins 1 vendeur."
                    });
                }

                int vendorId;
                if (p.VendorId.HasValue)
                {
                    var vendorExists = await _db.Vendors
                        .AsNoTracking()
                        .AnyAsync(v => v.Id == p.VendorId.Value);

                    vendorId = vendorExists ? p.VendorId.Value : fallbackVendorId;
                }
                else
                {
                    vendorId = fallbackVendorId;
                }

                decimal rate = await _commission.GetEffectiveRateAsync(p.CategoryId);
                if (rate <= 0m) rate = 0.10m;
                if (rate > 1m) rate /= 100m;

                var commissionAmount = Math.Round(lineTotal * rate, 2, MidpointRounding.AwayFromZero);
                var vendorNet = lineTotal - commissionAmount;

                var variant = ResolveVariant(p, it.VariantId, it.Size, it.Color);

                if (variant != null)
                {
                    if (variant.Stock < it.Qty)
                    {
                        return BadRequest(new
                        {
                            ok = false,
                            message = $"Stock insuffisant pour la variante de {p.Name}"
                        });
                    }

                    variant.Stock -= it.Qty;
                    variant.UpdatedAt = DateTime.UtcNow;

                    p.Stock = p.Variants.Sum(x => x.Stock);
                    p.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    if (p.Variants != null && p.Variants.Count > 0)
                    {
                        return BadRequest(new
                        {
                            ok = false,
                            message = $"Variante introuvable pour {p.Name}"
                        });
                    }

                    if (p.Stock < it.Qty)
                    {
                        return BadRequest(new
                        {
                            ok = false,
                            message = $"Stock insuffisant pour {p.Name}"
                        });
                    }

                    p.Stock -= it.Qty;
                    p.UpdatedAt = DateTime.UtcNow;
                }

                orderItems.Add(new OrderItem
                {
                    ProductId = p.Id,
                    VariantId = variant?.Id,
                    ProductName = p.Name,
                    UnitPrice = unit,
                    Quantity = it.Qty,

                    SelectedColor = variant != null
                        ? (string.IsNullOrWhiteSpace(variant.Color) ? null : variant.Color)
                        : (string.IsNullOrWhiteSpace(it.Color) ? null : it.Color),

                    SelectedSize = variant != null
                        ? (string.IsNullOrWhiteSpace(variant.Size) ? null : variant.Size)
                        : (string.IsNullOrWhiteSpace(it.Size) ? null : it.Size),

                    SkuSnapshot = p.Sku,
                    WeightKgSnapshot = p.WeightKg,
                    DimensionsSnapshot = p.Dimensions,

                    VendorId = vendorId,
                    UnitPriceSnapshot = unit,

                    PlatformFee = commissionAmount,
                    VendorAmount = vendorNet,
                    CommissionRate = rate,
                    CommissionRateSnapshot = rate,
                    CommissionAmount = commissionAmount,
                    VendorNetAmount = vendorNet,
                    VendorStatus = "Pending",
                    IsSeenByVendor = false,
                });
            }

            var delivery = dto.DeliveryFee < 0 ? 0 : dto.DeliveryFee;
            var total = subTotal + delivery;

            int? customerId = null;
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (string.Equals(role, "Client", StringComparison.OrdinalIgnoreCase))
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrWhiteSpace(idStr) && int.TryParse(idStr, out var cid))
                {
                    customerId = cid;
                }
                else
                {
                    var emailFromToken = User.FindFirstValue(ClaimTypes.Email);

                    if (!string.IsNullOrWhiteSpace(emailFromToken))
                    {
                        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Email == emailFromToken);
                        if (customer != null) customerId = customer.Id;
                    }
                }
            }

            var order = new Order
            {
                FullName = dto.FullName.Trim(),
                Phone = dto.Phone.Trim(),
                Email = dto.Email.Trim(),
                Address = dto.Address.Trim(),
                City = dto.City.Trim(),
                Note = dto.Note?.Trim(),
                DeliveryFee = delivery,
                SubTotal = subTotal,
                Total = total,
                Status = "EnAttente",
                Items = orderItems,
                CustomerId = customerId
            };

            _db.Orders.Add(order);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            await _push.SendNewOrderToAdminsAsync(order.Id, order.City, order.FullName, order.Total);
            return Ok(new
            {
                ok = true,
                message = "Commande enregistrée.",
                orderId = order.Id
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();

            var e1 = ex.Message;
            var e2 = ex.InnerException?.Message;
            var e3 = ex.InnerException?.InnerException?.Message;

            Console.WriteLine("❌ ORDER CREATE ERROR:");
            Console.WriteLine("LEVEL 1: " + e1);
            Console.WriteLine("LEVEL 2: " + e2);
            Console.WriteLine("LEVEL 3: " + e3);
            Console.WriteLine(ex.ToString());

            return StatusCode(500, new
            {
                ok = false,
                message = e1,
                inner = e2,
                inner2 = e3
            });
        }
    }

    [Authorize(Roles = "SuperAdmin,Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.Orders
            .OrderByDescending(o => o.Id)
            .Select(o => new
            {
                o.Id,
                o.FullName,
                o.Phone,
                o.Email,
                o.City,
                o.Address,
                o.SubTotal,
                o.DeliveryFee,
                o.Total,
                o.Status,
                ItemsCount = o.Items.Count
            })
            .ToListAsync();

        return Ok(new { ok = true, items = list });
    }

    private ProductVariant? ResolveVariant(Product p, int? variantId, string? sizeRaw, string? colorRaw)
    {
        if (p.Variants == null || p.Variants.Count == 0)
            return null;

        // ✅ priorité à variantId
        if (variantId.HasValue && variantId.Value > 0)
        {
            var byId = p.Variants.FirstOrDefault(v => v.Id == variantId.Value);
            if (byId != null) return byId;
        }

        // ✅ fallback ancien système pendant transition
        return FindMatchingVariant(p, sizeRaw, colorRaw);
    }

    private string BuildEmailBody(Order order)
    {
        var lines = string.Join("\n", order.Items.Select(i =>
        {
            var opts = "";
            if (!string.IsNullOrWhiteSpace(i.SelectedSize)) opts += $" | Taille: {i.SelectedSize}";
            if (!string.IsNullOrWhiteSpace(i.SelectedColor)) opts += $" | Couleur: {i.SelectedColor}";

            var ship = "";
            if (!string.IsNullOrWhiteSpace(i.SkuSnapshot)) ship += $" | SKU: {i.SkuSnapshot}";
            if (i.WeightKgSnapshot.HasValue) ship += $" | Poids: {i.WeightKgSnapshot.Value} kg";
            if (!string.IsNullOrWhiteSpace(i.DimensionsSnapshot)) ship += $" | Dim: {i.DimensionsSnapshot}";

            var lineTotal = i.UnitPrice * i.Quantity;
            return $"- {i.ProductName} x{i.Quantity} = {lineTotal} FCFA{opts}{ship}";
        }));

        return
$@"Nouvelle commande reçue ✅

Boutique : Ranita

Client : {order.FullName}
Téléphone : {order.Phone}
Email : {order.Email}
Ville : {order.City}
Adresse : {order.Address}

Produits :
{lines}

Total panier : {order.SubTotal} FCFA
Livraison : {order.DeliveryFee} FCFA
TOTAL FINAL : {order.Total} FCFA
";
    }

    private string BuildClientConfirmBody(Order order)
    {
        var lines = string.Join("\n", order.Items.Select(i =>
        {
            var lineTotal = i.UnitPrice * i.Quantity;
            return $"- {i.ProductName} x{i.Quantity} = {lineTotal} FCFA";
        }));

        return
$@"Bonjour {order.FullName},

✅ Ranita a bien reçu votre commande.
Notre équipe vous contactera très rapidement pour la livraison.

Détails :
Téléphone : {order.Phone}
Ville : {order.City}
Adresse : {order.Address}

Produits :
{lines}

Total panier : {order.SubTotal} FCFA
Livraison : {order.DeliveryFee} FCFA
TOTAL FINAL : {order.Total} FCFA

Merci pour votre confiance 🙏
Ma Boutique";
    }

    private static decimal EffectivePrice(Product p)
    {
        var price = p.Price;
        var promo = p.PricePromo ?? 0m;
        return (promo > 0m && promo < price) ? promo : price;
    }

    private ProductVariant? FindMatchingVariant(Product p, string? sizeRaw, string? colorRaw)
    {
        static string N(string? s) => (s ?? "").Trim().ToLowerInvariant();

        var size = N(sizeRaw);
        var color = N(colorRaw);

        if (p.Variants == null || p.Variants.Count == 0)
            return null;

        return p.Variants.FirstOrDefault(v =>
        {
            var vs = N(v.Size);
            var vc = N(v.Color);

            return
                (vs == size && vc == color) ||
                (string.IsNullOrEmpty(size) && vc == color) ||
                (string.IsNullOrEmpty(size) && vs == color) ||
                (string.IsNullOrEmpty(size) && vs == color && vc == "unique") ||
                (string.IsNullOrEmpty(size) && vc == color && vs == "unique") ||
                (string.IsNullOrEmpty(color) && vs == size) ||
                (string.IsNullOrEmpty(color) && vc == size);
        });
    }
}