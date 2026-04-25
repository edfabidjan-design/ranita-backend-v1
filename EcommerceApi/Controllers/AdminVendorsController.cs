using EcommerceApi.Data;
using EcommerceApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/admin/vendors")]
[Authorize(Roles = "SUPER_ADMIN,ADMIN,MANAGER")]
public class AdminVendorsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly EmailService _email;
    private readonly IConfiguration _config;
    public AdminVendorsController(AppDbContext db, EmailService email, IConfiguration config)
    {
        _db = db;
        _email = email;
        _config = config;
    }

    // GET /api/admin/vendors?status=0  (0=attente,1=validé,2=refusé,3=retiré, -1=tous)
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? status = null)
    {
        var q = _db.Vendors.AsQueryable();

        if (status.HasValue && status.Value >= 0)
            q = q.Where(v => v.Status == status.Value);

        var items = await q
            .OrderByDescending(v => v.CreatedAt)
.Select(v => new
{
    id = v.Id,
    name = v.Name,
    slug = v.Slug,
    email = v.Email,
    phone = v.Phone,
    status = v.Status,
    createdAt = v.CreatedAt,
    commissionRate = v.CommissionRate,

    termsEmailSent = v.TermsEmailSent,
    termsEmailSentAt = v.TermsEmailSentAt,

    signedAgreementReceived = v.SignedAgreementReceived,
    signedAgreementReceivedAt = v.SignedAgreementReceivedAt,

    contractPdfPath = v.ContractPdfPath,

    // 🔥 AJOUT CRITIQUE
    signedContractPath = v.SignedContractPath,
    signedContractReceivedAt = v.SignedContractReceivedAt
})
            .ToListAsync();

        return Ok(new { ok = true, items });
    }

    // POST /api/admin/vendors/{id}/approve
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var v = await _db.Vendors.FirstOrDefaultAsync(x => x.Id == id);

        if (v == null)
            return NotFound(new { ok = false });

        // 🚨 BLOQUAGE IMPORTANT
        if (string.IsNullOrWhiteSpace(v.SignedContractPath))
        {
            return BadRequest(new
            {
                ok = false,
                message = "Le contrat signé n'a pas été reçu."
            });
        }

        v.Status = 1;
        await _db.SaveChangesAsync();

        // ✅ ENVOI MAIL (AJOUT ICI)
        await TrySendVendorEmail(
            v.Email,
            "Ranita — Boutique validée ✅",
            $@"Bonjour {v.Name},

Votre boutique a été VALIDÉE avec succès 🎉

Vous pouvez maintenant vous connecter à votre espace vendeur et publier vos produits.

👉 Accès vendeur : https://tonsite.com/vendor-login.html

Merci,
L’équipe Ranita"
        );

        return Ok(new { ok = true });
    }

    // POST /api/admin/vendors/{id}/reject
    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectDto? dto)
    {
        var v = await _db.Vendors.FirstOrDefaultAsync(x => x.Id == id);
        if (v == null) return NotFound(new { ok = false, message = "Boutique introuvable." });

        v.Status = 2; // Refusé

        // ✅ on garde le PDF pour consultation
        // ❌ mais on enlève le signal "nouveau contrat reçu"
        v.SignedContractReceivedAt = null;

        await _db.SaveChangesAsync();

        var reason = (dto?.Reason ?? "").Trim();
        var reasonTxt = string.IsNullOrWhiteSpace(reason) ? "" : $"\n\nMotif : {reason}";

        await TrySendVendorEmail(v.Email,
            "Ranita — Boutique refusée ❌",
            $@"Bonjour {v.Name},

Votre demande de boutique a été REFUSÉE.{reasonTxt}

Vous pouvez corriger les informations et refaire une demande.

Merci,
L’équipe Ranita");

        return Ok(new { ok = true, message = "Boutique refusée." });
    }

    public record RejectDto(string? Reason);

    // POST /api/admin/vendors/{id}/disable?disableProducts=true
    [HttpPost("{id:int}/disable")]
    public async Task<IActionResult> Disable(int id, [FromQuery] bool disableProducts = true)
    {
        var v = await _db.Vendors.FirstOrDefaultAsync(x => x.Id == id);
        if (v == null) return NotFound(new { ok = false, message = "Boutique introuvable." });

        v.Status = 3; // ✅ Retiré (différent de Refusé)

        if (disableProducts)
        {
            var prods = await _db.Products
                .Where(p => p.VendorId == id && !p.IsDeleted)
                .ToListAsync();

            foreach (var p in prods)
            {
                p.IsActive = false;
                p.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();

        await TrySendVendorEmail(v.Email,
            "Ranita — Boutique retirée ⚠️",
            $@"Bonjour {v.Name},

Votre boutique a été RETIRÉE par l’administration.
Vos produits ont été désactivés et ne sont plus visibles.

Si vous pensez qu’il s’agit d’une erreur, contactez le support Ranita.

Merci,
L’équipe Ranita");

        return Ok(new { ok = true, message = "Vendeur retiré." });
    }

    // POST /api/admin/vendors/{id}/enable?enableProducts=true
    [HttpPost("{id:int}/enable")]
    public async Task<IActionResult> Enable(int id, [FromQuery] bool enableProducts = true)
    {
        var v = await _db.Vendors.FirstOrDefaultAsync(x => x.Id == id);
        if (v == null) return NotFound(new { ok = false, message = "Boutique introuvable." });

        v.Status = 1; // ✅ Réactivé => Validé

        if (enableProducts)
        {
            var prods = await _db.Products
                .Where(p => p.VendorId == id)
                .ToListAsync();

            foreach (var p in prods)
            {
                p.IsDeleted = false;
                p.IsActive = true;
                p.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();

        await TrySendVendorEmail(v.Email,
            "Ranita — Boutique réactivée ✅",
            $@"Bonjour {v.Name},

Votre boutique a été RÉACTIVÉE.
Vos produits peuvent redevenir visibles (selon leur état de publication).

Merci,
L’équipe Ranita");

        return Ok(new { ok = true, message = "Vendeur réactivé." });
    }

    private async Task TrySendVendorEmail(string? to, string subject, string body)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(to))
                await _email.SendAsync(to, subject, body);
        }
        catch
        {
            // ne pas bloquer l’action admin si l’email échoue
        }
    }

    [HttpPost("{id:int}/resend-terms")]
    public async Task<IActionResult> ResendTerms(int id)
    {
        var v = await _db.Vendors.FirstOrDefaultAsync(x => x.Id == id);
        if (v == null)
            return NotFound(new { ok = false, message = "Boutique introuvable." });

        try
        {
            await _email.SendVendorTermsEmailAsync(
                v.Email!,
                v.Name,
                v.CommissionRate
            );

            v.TermsEmailSent = true;
            v.TermsEmailSentAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        catch
        {
            return StatusCode(500, new { ok = false, message = "Erreur envoi email." });
        }

        return Ok(new { ok = true, message = "Email renvoyé." });
    }


    [HttpPost("{id:int}/mark-agreement-received")]
    public async Task<IActionResult> MarkAgreementReceived(int id)
    {
        var v = await _db.Vendors.FirstOrDefaultAsync(x => x.Id == id);
        if (v == null)
            return NotFound(new { ok = false, message = "Boutique introuvable." });

        v.SignedAgreementReceived = true;
        v.SignedAgreementReceivedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { ok = true, message = "Accord signé marqué comme reçu." });
    }

    [HttpPost("{id:int}/unmark-agreement-received")]
    public async Task<IActionResult> UnmarkAgreementReceived(int id)
    {
        var v = await _db.Vendors.FirstOrDefaultAsync(x => x.Id == id);
        if (v == null)
            return NotFound(new { ok = false, message = "Boutique introuvable." });

        v.SignedAgreementReceived = false;
        v.SignedAgreementReceivedAt = null;

        await _db.SaveChangesAsync();

        return Ok(new { ok = true, message = "Accord signé retiré." });
    }


    public class DeleteVendorDto
    {
        public string Password { get; set; }
    }


    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteVendor(int id, [FromBody] DeleteVendorDto dto)
    {
        try
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { ok = false, message = "Mot de passe requis." });

            var adminDeletePassword = _config["BootstrapAdmin:Password"];
            if (string.IsNullOrWhiteSpace(adminDeletePassword))
                return StatusCode(500, new { ok = false, message = "Mot de passe de sécurité non configuré." });

            if (dto.Password != adminDeletePassword)
                return Unauthorized(new { ok = false, message = "Mot de passe incorrect." });

            var v = await _db.Vendors.FirstOrDefaultAsync(x => x.Id == id);
            if (v == null)
                return NotFound(new { ok = false, message = "Vendeur introuvable." });

            var productIds = await _db.Products
                .Where(x => x.VendorId == id)
                .Select(x => x.Id)
                .ToListAsync();

            var orderItemIds = await _db.OrderItems
                .Where(x => x.VendorId == id || productIds.Contains(x.ProductId))
                .Select(x => x.Id)
                .Distinct()
                .ToListAsync();

            var returnRequestIds = await _db.ReturnItems
                .Where(x => orderItemIds.Contains(x.OrderItemId))
                .Select(x => x.ReturnRequestId)
                .Distinct()
                .ToListAsync();

            var returnImages = _db.ReturnImages
                .Where(x => returnRequestIds.Contains(x.ReturnRequestId));
            _db.ReturnImages.RemoveRange(returnImages);

            var returnItems = _db.ReturnItems
                .Where(x => orderItemIds.Contains(x.OrderItemId));
            _db.ReturnItems.RemoveRange(returnItems);

            var returnRequests = _db.ReturnRequests
                .Where(x => returnRequestIds.Contains(x.Id));
            _db.ReturnRequests.RemoveRange(returnRequests);

            var adminWalletTx = _db.AdminWalletTransactions
                .Where(x => x.OrderItemId != null && orderItemIds.Contains(x.OrderItemId.Value));
            _db.AdminWalletTransactions.RemoveRange(adminWalletTx);

            var orderItems = _db.OrderItems
                .Where(x => x.VendorId == id || productIds.Contains(x.ProductId));
            _db.OrderItems.RemoveRange(orderItems);

            var flashDeals = _db.FlashDeals
                .Where(x => productIds.Contains(x.ProductId));
            _db.FlashDeals.RemoveRange(flashDeals);

            var productImages = _db.ProductImages
                .Where(x => productIds.Contains(x.ProductId));
            _db.ProductImages.RemoveRange(productImages);

            // 1) récupérer les IDs des variants
            var variantIds = await _db.ProductVariants
                .Where(x => productIds.Contains(x.ProductId))
                .Select(x => x.Id)
                .ToListAsync();

            // 2) supprimer les valeurs de variants
            var productVariantValues = _db.ProductVariantValues
                .Where(x => variantIds.Contains(x.ProductVariantId));

            _db.ProductVariantValues.RemoveRange(productVariantValues);


            var productVariants = _db.ProductVariants
                .Where(x => productIds.Contains(x.ProductId));
            _db.ProductVariants.RemoveRange(productVariants);

            var productAttributeValues = _db.ProductAttributeValues
                .Where(x => x.ProductId != null && productIds.Contains(x.ProductId))
                .ToList();
            _db.ProductAttributeValues.RemoveRange(productAttributeValues);

            var productReviews = _db.ProductReviews
                .Where(x => productIds.Contains(x.ProductId));
            _db.ProductReviews.RemoveRange(productReviews);

            var favorites = _db.Favorites
                .Where(x => productIds.Contains(x.ProductId));
            _db.Favorites.RemoveRange(favorites);

            var products = _db.Products
                .Where(x => x.VendorId == id);
            _db.Products.RemoveRange(products);

            var users = _db.VendorUsers
                .Where(x => x.VendorId == id);
            _db.VendorUsers.RemoveRange(users);

            var wallet = _db.VendorWalletTransactions
                .Where(x => x.VendorId == id);
            _db.VendorWalletTransactions.RemoveRange(wallet);

            var stats1 = _db.VendorProductDailyStats
                .Where(x => x.VendorId == id);
            _db.VendorProductDailyStats.RemoveRange(stats1);

            var stats2 = _db.VendorDailyStats
                .Where(x => x.VendorId == id);
            _db.VendorDailyStats.RemoveRange(stats2);



            var payouts = _db.VendorPayouts
                .Where(x => x.VendorId == id);
            _db.VendorPayouts.RemoveRange(payouts);

            var vendorAccounts = _db.VendorAccounts
                .Where(x => x.VendorId == id);
            _db.VendorAccounts.RemoveRange(vendorAccounts);



            var resets = _db.VendorPasswordResets
                .Where(x => x.VendorId == id);
            _db.VendorPasswordResets.RemoveRange(resets);

            _db.Vendors.Remove(v);

            await _db.SaveChangesAsync();

            return Ok(new { ok = true, message = "Vendeur supprimé définitivement." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                ok = false,
                message = ex.Message,
                detail = ex.InnerException?.Message,
                debug = ex.ToString()
            });
        }
    }
}