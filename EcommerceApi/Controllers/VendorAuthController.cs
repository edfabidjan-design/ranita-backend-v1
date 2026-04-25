using EcommerceApi.Data;
using EcommerceApi.Dtos;
using EcommerceApi.Models;
using EcommerceApi.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using static EcommerceApi.Dtos.VendorAuthDtos;
using Microsoft.AspNetCore.Authorization;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/vendor-auth")]
public class VendorAuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    private readonly EmailService _email;
    private readonly VendorContractPdfService _vendorContractPdf;
    private readonly PushService _push;
    private readonly IWebHostEnvironment _env;

    public VendorAuthController(
        AppDbContext db,
        IConfiguration cfg,
        EmailService email,
        VendorContractPdfService vendorContractPdf,
        PushService push,
        IWebHostEnvironment env)
    {
        _db = db;
        _cfg = cfg;
        _email = email;
        _vendorContractPdf = vendorContractPdf;
        _push = push;
        _env = env;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] VendorRegisterDto dto)
    {
        if (dto is null) return BadRequest(new { ok = false, message = "Données invalides." });

        var vendorName = (dto.VendorName ?? "").Trim();
        var email = (dto.Email ?? "").Trim().ToLowerInvariant();
        var pwd = dto.Password ?? "";

        var phone = (dto.Phone ?? "").Trim();

        if (string.IsNullOrWhiteSpace(vendorName))
            return BadRequest(new { ok = false, message = "Nom boutique obligatoire." });

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { ok = false, message = "Email obligatoire." });

        if (!Regex.IsMatch(email, @"^\S+@\S+\.\S+$"))
            return BadRequest(new { ok = false, message = "Email invalide." });

        if (pwd.Length < 6)
            return BadRequest(new { ok = false, message = "Mot de passe min 6 caractères." });

        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest(new { ok = false, message = "Téléphone obligatoire." });

        // ✅ évite doublon email (VendorUsers)
        var emailExists = await _db.VendorUsers.AnyAsync(x => x.Email.ToLower() == email);
        if (emailExists)
            return BadRequest(new { ok = false, message = "Email déjà utilisé." });

        // ✅ (optionnel mais bien) évite doublon email (Vendors)
        var vendorEmailExists = await _db.Vendors.AnyAsync(v => v.Email.ToLower() == email);
        if (vendorEmailExists)
            return BadRequest(new { ok = false, message = "Email déjà utilisé (boutique)." });

        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            var slugBase = Slugify(vendorName);
            var slug = await MakeUniqueVendorSlug(slugBase);

            var vendor = new Vendor
            {
                Name = vendorName,
                Slug = slug,
                Email = email,
                Phone = phone,
                Status = 0, // EnAttente
                CreatedAt = DateTime.UtcNow,
                 CommissionRate = 12
            };

            // 1. Création vendeur
            _db.Vendors.Add(vendor);
            await _db.SaveChangesAsync();

            // 🔔 NOTIFICATION ADMIN (DEMANDE BOUTIQUE)
            _db.AdminNotifications.Add(new AdminNotification
            {
                Type = "VendorRequest",
                RefId = vendor.Id,
                Title = "Nouvelle demande de boutique",
                Message = vendor.Name,
                Body = $"La boutique {vendor.Name} a été soumise.",
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            // 🔔 PUSH FCM
            await _push.SendNewVendorRequestToAdminsAsync(
                vendor.Id,
                vendor.Name,
                vendor.Name // ou ownerName si tu l'ajoutes plus tard
            );
            // 2. Génération PDF + envoi email
            try
            {
                var pdfBytes = _vendorContractPdf.GenerateContractPdf(
                    vendor.Name,
                    vendor.Email ?? "",
                    vendor.Phone ?? "",
                    vendor.CommissionRate
                );

                // 👉 BONUS : sauvegarde PDF sur serveur
                var folder = Path.Combine(
                    _env.WebRootPath,
                    "uploads",
                    "vendor-contracts"
                );
                Directory.CreateDirectory(folder);

                var filePath = Path.Combine(folder, $"Contrat_Vendeur_{vendor.Id}.pdf");
                await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);

                // 👉 sauvegarde chemin
                vendor.ContractPdfPath = $"/uploads/vendor-contracts/Contrat_Vendeur_{vendor.Id}.pdf";

                var html = $@"
<h2>Bonjour {vendor.Name},</h2>
<p>Votre demande de boutique a bien été enregistrée.</p>
<p>Vous trouverez en pièce jointe le contrat vendeur Ranita.</p>
<p>Merci de le signer puis de le renvoyer à l'équipe Ranita pour validation.</p>
<p><b>Commission :</b> {vendor.CommissionRate}%</p>
<p>Équipe Ranita</p>";

                await _email.SendWithAttachmentAsync(
                    vendor.Email!,
                    "Ranita - Contrat vendeur à signer",
                    html,
                    pdfBytes,
                    $"Contrat_Vendeur_{vendor.Name.Replace(" ", "_")}.pdf"
                );

                // 👉 tracking admin
                vendor.TermsEmailSent = true;
                vendor.TermsEmailSentAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
            }
            catch
            {
                // ne pas bloquer l'inscription si email échoue
            }


            var usernameBase = MakeUsernameFromEmail(email);
            var username = await MakeUniqueVendorUsername(vendor.Id, usernameBase);

            var vu = new VendorUser
            {
                VendorId = vendor.Id,
                Username = username,
                Email = email,
                IsOwner = true,
                IsActive = true,
                Role = "Vendor",
                CreatedAt = DateTime.UtcNow,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(pwd)
            };

            _db.VendorUsers.Add(vu);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();

            try
            {
                await _email.SendVendorTermsEmailAsync(
                    vendor.Email!,
                    vendor.Name,
                    vendor.CommissionRate
                );

                vendor.TermsEmailSent = true;
                vendor.TermsEmailSentAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            catch
            {
                // ne pas bloquer l'inscription si l'email échoue
            }

            var token = BuildJwt(vu.Id, vu.Email, "Vendor", vendor.Id);

            return Ok(new
            {
                ok = true,
                message = "Compte vendeur créé. En attente de validation par admin.",
                token,
                user = new
                {
                    id = vu.Id,
                    email = vu.Email,
                    role = "Vendor",
                    vendorId = vendor.Id,
                    vendorStatus = vendor.Status,
                    vendorName = vendor.Name,
                    contractPdfPath = vendor.ContractPdfPath,
                    signedContractPath = vendor.SignedContractPath
                }
            });
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();
            // ici tu peux logger ex.InnerException?.Message
            return StatusCode(500, new { ok = false, message = "Erreur serveur (DB). Vérifie migration + contraintes." });
        }
        catch (Exception)
        {
            await tx.RollbackAsync();
            return StatusCode(500, new { ok = false, message = "Erreur serveur." });
        }
    }

    private async Task<string> MakeUniqueVendorUsername(int vendorId, string baseUsername)
    {
        var u = baseUsername;
        var i = 1;

        while (await _db.VendorUsers.AnyAsync(x => x.VendorId == vendorId && x.Username == u))
        {
            i++;
            u = $"{baseUsername}{i}";
        }
        return u;
    }

    private async Task<string> MakeUniqueVendorSlug(string baseSlug)
    {
        var s = baseSlug;
        var i = 1;

        while (await _db.Vendors.AnyAsync(v => v.Slug == s))
        {
            i++;
            s = $"{baseSlug}-{i}";
        }
        return s;
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] VendorLoginDto dto)
    {
        if (dto is null) return BadRequest(new { ok = false, message = "Données invalides." });

        var email = (dto.Email ?? "").Trim().ToLowerInvariant();
        var pwd = dto.Password ?? "";

        var vu = await _db.VendorUsers
            .Include(x => x.Vendor)
            .FirstOrDefaultAsync(x =>
                x.Email.ToLower() == email &&
                x.IsActive == true &&
                x.Vendor.Status != 2
            );

        if (vu is null)
            return Unauthorized(new { ok = false, message = "Email ou mot de passe incorrect." });

        if (!BCrypt.Net.BCrypt.Verify(pwd, vu.PasswordHash))
            return Unauthorized(new { ok = false, message = "Email ou mot de passe incorrect." });

        vu.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = BuildJwt(vu.Id, vu.Email, "Vendor", vu.VendorId);

        return Ok(new
        {
            ok = true,
            token,
            user = new
            {
                id = vu.Id,
                email = vu.Email,
                role = "Vendor",
                vendorId = vu.VendorId,
                vendorStatus = vu.Vendor.Status,
                vendorName = vu.Vendor.Name,
                contractPdfPath = vu.Vendor.ContractPdfPath,
                signedContractPath = vu.Vendor.SignedContractPath
            }
        });
    }

    private string BuildJwt(int vendorUserId, string email, string role, int vendorId)
    {
        var key = _cfg["JwtVendor:Key"] ?? _cfg["Jwt:Key"] ?? throw new Exception("Clé JWT manquante");
        var issuer = _cfg["JwtVendor:Issuer"] ?? _cfg["Jwt:Issuer"];
        var audience = _cfg["JwtVendor:Audience"] ?? _cfg["Jwt:Audience"];

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, vendorUserId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim("vendorId", vendorId.ToString()),
            new Claim("vendorUserId", vendorUserId.ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static string Slugify(string s)
    {
        s = (s ?? "").Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"\s+", "-");
        s = Regex.Replace(s, @"[^a-z0-9\-]", "");
        s = Regex.Replace(s, @"-+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(s) ? "vendor" : s;
    }

    private static string MakeUsernameFromEmail(string email)
    {
        var u = (email ?? "").Split('@')[0];
        u = Regex.Replace(u, @"[^a-zA-Z0-9_\.]", "");
        u = u.Trim('.', '_');
        return string.IsNullOrWhiteSpace(u) ? "vendor" : u.ToLowerInvariant();
    }

    private async Task<string> MakeUniqueVendorUsername(string baseUsername)
    {
        var u = baseUsername;
        var i = 1;
        while (await _db.VendorUsers.AnyAsync(x => x.Username == u))
        {
            i++;
            u = $"{baseUsername}{i}";
        }
        return u;
    }


    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] VendorForgotPasswordDto dto)
    {
        var email = (dto.Email ?? "").Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { ok = false, message = "Email obligatoire." });

        var vendor = await _db.Vendors
            .FirstOrDefaultAsync(x => x.Email != null && x.Email.ToLower() == email);

        if (vendor == null)
        {
            return Ok(new
            {
                ok = true,
                message = "Si un compte vendeur existe, un code de réinitialisation a été envoyé."
            });
        }

        var oldCodes = await _db.VendorPasswordResets
            .Where(x => x.VendorId == vendor.Id && !x.IsUsed)
            .ToListAsync();

        foreach (var item in oldCodes)
            item.IsUsed = true;

        var code = new Random().Next(100000, 999999).ToString();

        _db.VendorPasswordResets.Add(new VendorPasswordReset
        {
            VendorId = vendor.Id,
            Email = email,
            ResetCode = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        await _email.SendAsync(
            vendor.Email!,
            "Réinitialisation du mot de passe vendeur - Ranita",
            $@"
<div style='font-family:Arial,sans-serif;background:#0b1220;padding:24px;color:#e5e7eb'>
    <div style='max-width:520px;margin:auto;background:#111827;border:1px solid #1f2937;border-radius:14px;padding:24px'>
        <h2 style='margin-top:0;color:#ffffff'>🔐 Réinitialisation vendeur</h2>
        <p>Votre code vendeur Ranita est :</p>
        <div style='margin:20px 0;padding:14px 18px;background:#facc15;color:#111827;
                    font-size:28px;font-weight:900;text-align:center;border-radius:10px'>
            {code}
        </div>
        <p>Ce code expire dans <strong>15 minutes</strong>.</p>
    </div>
</div>"
        );

        return Ok(new
        {
            ok = true,
            message = "Si un compte vendeur existe, un code de réinitialisation a été envoyé."
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] VendorResetPasswordDto dto)
    {
        var email = (dto.Email ?? "").Trim().ToLowerInvariant();
        var code = (dto.Code ?? "").Trim();
        var newPassword = dto.NewPassword ?? "";
        var confirmPassword = dto.ConfirmPassword ?? "";

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { ok = false, message = "Email obligatoire." });

        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { ok = false, message = "Code obligatoire." });

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
            return BadRequest(new { ok = false, message = "Mot de passe trop court." });

        if (newPassword != confirmPassword)
            return BadRequest(new { ok = false, message = "La confirmation du mot de passe est incorrecte." });

        var vendor = await _db.Vendors
            .FirstOrDefaultAsync(x => x.Email != null && x.Email.ToLower() == email);

        if (vendor == null)
            return BadRequest(new { ok = false, message = "Compte vendeur introuvable." });

        var reset = await _db.VendorPasswordResets
            .Where(x => x.VendorId == vendor.Id
                        && x.Email == email
                        && x.ResetCode == code
                        && !x.IsUsed)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        if (reset == null)
            return BadRequest(new { ok = false, message = "Code invalide." });

        if (reset.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { ok = false, message = "Code expiré." });

        var vendorUser = await _db.VendorUsers
            .FirstOrDefaultAsync(x => x.VendorId == vendor.Id && x.Email.ToLower() == email && x.IsActive);

        if (vendorUser == null)
            return BadRequest(new { ok = false, message = "Utilisateur vendeur introuvable." });

        vendorUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        reset.IsUsed = true;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            ok = true,
            message = "Mot de passe vendeur réinitialisé avec succès."
        });
    }


 
    [Authorize(Roles = "Vendor")]
    [HttpPost("upload-signed-contract")]
    public async Task<IActionResult> UploadSignedContract(IFormFile file)
    {
        try
        {
            var vendorIdClaim = User.FindFirst("vendorId")?.Value;
            if (string.IsNullOrWhiteSpace(vendorIdClaim))
                return Unauthorized(new { ok = false, message = "Vendor non identifié." });

            if (!int.TryParse(vendorIdClaim, out var vendorId))
                return Unauthorized(new { ok = false, message = "Vendor invalide." });

            if (file == null || file.Length == 0)
                return BadRequest(new { ok = false, message = "Fichier manquant." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".pdf")
                return BadRequest(new { ok = false, message = "Seuls les PDF sont acceptés." });

            var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId);
            if (vendor == null)
                return NotFound(new { ok = false, message = "Vendeur introuvable." });

            var folder = Path.Combine(_env.WebRootPath, "uploads", "signed-contracts");
            Directory.CreateDirectory(folder);

            var fileName = $"signed_contract_vendor_{vendor.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            var fullPath = Path.Combine(folder, fileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            vendor.SignedContractPath = $"/uploads/signed-contracts/{fileName}";
            vendor.SignedAgreementReceived = true;
            vendor.SignedAgreementReceivedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                ok = true,
                message = "Contrat signé envoyé avec succès.",
                path = vendor.SignedContractPath
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("UPLOAD ERROR: " + ex);

            return StatusCode(500, new
            {
                ok = false,
                message = "Erreur serveur upload contrat.",
                detail = ex.Message
            });
        }
    }
}
