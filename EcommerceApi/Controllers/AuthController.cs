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


namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    private readonly EmailService _email;
    private readonly WhatsAppService _whatsapp;

    public AuthController(AppDbContext db, IConfiguration cfg, EmailService email, WhatsAppService whatsapp)
    {
        _db = db;
        _cfg = cfg;
        _email = email;
        _whatsapp = whatsapp;
    }

    // =========================
    // ADMIN LOGIN
    // POST /api/auth/login
    // =========================
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { ok = false, message = "Username et Password obligatoires." });

        var username = dto.Username.Trim();

        var user = await _db.Users
            .Include(x => x.RoleRef)
            .FirstOrDefaultAsync(x => x.Username == username && x.IsActive);

        if (user == null)
            return Unauthorized(new { ok = false, message = "Identifiants invalides." });

        var okPwd = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!okPwd)
            return Unauthorized(new { ok = false, message = "Identifiants invalides." });

        var roleCode = ResolveRoleCode(user);
        var roleLabel = ResolveRoleLabel(user);

        var (jwtToken, expUtc) = BuildAdminJwt(user, roleCode);

        return Ok(new
        {
            ok = true,
            token = jwtToken,
            expUtc,
            user = new
            {
                user.Id,
                user.Username,
                role = roleLabel,
                roleCode,
                roleId = user.RoleId,
                isActive = user.IsActive
            }
        });
    }

    private string ResolveRoleCode(User user)
    {
        if (!string.IsNullOrWhiteSpace(user.RoleRef?.Code))
            return user.RoleRef.Code.Trim().ToUpperInvariant();

        var legacyRole = (user.Role ?? "").Trim().ToLowerInvariant();

        return legacyRole switch
        {
            "superadmin" => "SUPER_ADMIN",
            "admin" => "ADMIN",
            "manager" => "MANAGER",
            _ => "ADMIN"
        };
    }

    private string ResolveRoleLabel(User user)
    {
        if (!string.IsNullOrWhiteSpace(user.RoleRef?.Name))
            return user.RoleRef.Name.Trim();

        return string.IsNullOrWhiteSpace(user.Role) ? "Admin" : user.Role.Trim();
    }

    private (string token, DateTime expUtc) BuildAdminJwt(User user, string roleCode)
    {
        var jwt = _cfg.GetSection("Jwt");
        var key = jwt["Key"] ?? throw new Exception("Jwt:Key manquant");
        var issuer = jwt["Issuer"] ?? "RanitaApi";
        var audience = jwt["Audience"] ?? "RanitaAdmin";
        var expireMinutes = int.TryParse(jwt["ExpireMinutes"], out var m) ? m : 720;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, roleCode),
            new Claim("username", user.Username)
        };

        if (user.RoleId.HasValue)
            claims.Add(new Claim("roleId", user.RoleId.Value.ToString()));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var expUtc = DateTime.UtcNow.AddMinutes(expireMinutes);

        var tokenObj = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expUtc,
            signingCredentials: creds
        );

        var token = new JwtSecurityTokenHandler().WriteToken(tokenObj);
        return (token, expUtc);
    }

    // =========================
    // CLIENT REGISTER
    // POST /api/auth/client/register
    // =========================
    [HttpPost("client/register")]
    public async Task<IActionResult> ClientRegister([FromBody] ClientRegisterDto dto)
    {
        if (dto == null)
            return BadRequest(new { ok = false, message = "Données invalides." });

        var fullName = (dto.FullName ?? "").Trim();
        var phone = NormalizePhone(dto.Phone);
        var email = NormalizeEmail(dto.Email);

        if (string.IsNullOrWhiteSpace(fullName))
            return BadRequest(new { ok = false, message = "Nom complet obligatoire." });

        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest(new { ok = false, message = "Téléphone obligatoire." });

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { ok = false, message = "Email obligatoire." });

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 4)
            return BadRequest(new { ok = false, message = "Mot de passe trop court." });

        if (await _db.Customers.AnyAsync(x => x.Phone == phone))
            return BadRequest(new { ok = false, message = "Ce téléphone est déjà utilisé." });

        if (await _db.Customers.AnyAsync(x => x.Email == email))
            return BadRequest(new { ok = false, message = "Cet email est déjà utilisé." });

        var c = new Customer
        {
            FullName = fullName,
            Phone = phone,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Customers.Add(c);
        await _db.SaveChangesAsync();

        var (token, expUtc) = BuildCustomerJwt(c);

        return Ok(new
        {
            ok = true,
            token,
            expUtc,
            user = new { c.Id, fullName = c.FullName, phone = c.Phone, email = c.Email, role = "Client" }
        });
    }

    // =========================
    // CLIENT LOGIN
    // POST /api/auth/client/login
    // =========================
    [HttpPost("client/login")]
    public async Task<IActionResult> ClientLogin([FromBody] ClientLoginDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Login) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { ok = false, message = "Login et Password obligatoires." });

        var raw = dto.Login.Trim();
        var phone = NormalizePhone(raw);
        var email = NormalizeEmail(raw);

        Customer? c = null;

        if (!string.IsNullOrWhiteSpace(phone))
            c = await _db.Customers.FirstOrDefaultAsync(x => x.Phone == phone && x.IsActive);

        if (c == null && !string.IsNullOrWhiteSpace(email))
            c = await _db.Customers.FirstOrDefaultAsync(x => x.Email == email && x.IsActive);

        if (c == null)
            return Unauthorized(new { ok = false, message = "Identifiants invalides." });

        var okPwd = BCrypt.Net.BCrypt.Verify(dto.Password, c.PasswordHash);
        if (!okPwd)
            return Unauthorized(new { ok = false, message = "Identifiants invalides." });

        var (token, expUtc) = BuildCustomerJwt(c);

        return Ok(new
        {
            ok = true,
            token,
            expUtc,
            user = new { c.Id, fullName = c.FullName, phone = c.Phone, email = c.Email, role = "Client" }
        });
    }

    private (string token, DateTime expUtc) BuildCustomerJwt(Customer c)
    {
        var jwt = _cfg.GetSection("Jwt");
        var key = jwt["Key"] ?? throw new Exception("Jwt:Key manquant");
        var issuer = jwt["Issuer"] ?? "RanitaApi";
        var audience = "RanitaClient";
        var expireMinutes = int.TryParse(jwt["ExpireMinutes"], out var m) ? m : 720;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, c.Id.ToString()),
            new Claim(ClaimTypes.Name, c.FullName),
            new Claim(ClaimTypes.Role, "Client"),
            new Claim("phone", c.Phone),
        };

        if (!string.IsNullOrWhiteSpace(c.Email))
            claims.Add(new Claim(ClaimTypes.Email, c.Email));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var expUtc = DateTime.UtcNow.AddMinutes(expireMinutes);

        var tokenObj = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expUtc,
            signingCredentials: creds
        );

        var token = new JwtSecurityTokenHandler().WriteToken(tokenObj);
        return (token, expUtc);
    }

    // =========================
    // Helpers
    // =========================
    private static string NormalizeEmail(string? s)
    {
        s = (s ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(s)) return "";

        if (!Regex.IsMatch(s, @"^\S+@\S+\.\S+$")) return "";
        return s;
    }

    private static string NormalizePhone(string? s)
    {
        s = (s ?? "").Trim();
        if (string.IsNullOrWhiteSpace(s)) return "";

        s = Regex.Replace(s, @"[\s\-\.\(\)]", "");

        if (s.StartsWith("+"))
        {
            var rest = Regex.Replace(s.Substring(1), @"\D", "");
            return rest.Length >= 6 ? "+" + rest : "";
        }

        var digits = Regex.Replace(s, @"\D", "");
        return digits.Length >= 6 ? digits : "";
    }

    [HttpPost("client/forgot-password")]
    public async Task<IActionResult> ClientForgotPassword([FromBody] ClientForgotPasswordDto dto)
    {
        var raw = (dto.Login ?? "").Trim();

        if (string.IsNullOrWhiteSpace(raw))
            return BadRequest(new { ok = false, message = "Email ou téléphone obligatoire." });

        var phone = NormalizePhone(raw);
        var email = NormalizeEmail(raw);

        Customer? customer = null;
        string loginValue = "";

        if (!string.IsNullOrWhiteSpace(phone))
        {
            customer = await _db.Customers.FirstOrDefaultAsync(x => x.Phone == phone && x.IsActive);
            if (customer != null) loginValue = phone;
        }

        if (customer == null && !string.IsNullOrWhiteSpace(email))
        {
            customer = await _db.Customers.FirstOrDefaultAsync(x => x.Email == email && x.IsActive);
            if (customer != null) loginValue = email;
        }

        // Réponse générique pour éviter de révéler si le compte existe
        if (customer == null)
        {
            return Ok(new
            {
                ok = true,
                message = "Si un compte existe, un code de réinitialisation a été envoyé."
            });
        }

        // Le reset par email nécessite un email sur le compte
        if (string.IsNullOrWhiteSpace(customer.Email))
        {
            return Ok(new
            {
                ok = true,
                message = "Si un compte existe, un code de réinitialisation a été envoyé."
            });
        }

        var oldCodes = await _db.CustomerPasswordResets
            .Where(x => x.CustomerId == customer.Id && !x.IsUsed)
            .ToListAsync();

        foreach (var item in oldCodes)
            item.IsUsed = true;

        var code = new Random().Next(100000, 999999).ToString();

        var reset = new CustomerPasswordReset
        {
            CustomerId = customer.Id,
            LoginValue = loginValue,
            ResetCode = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.CustomerPasswordResets.Add(reset);
        await _db.SaveChangesAsync();

        await _email.SendAsync(
            customer.Email,
            "Réinitialisation du mot de passe - Ranita",
            $@"
<div style='font-family:Arial,sans-serif;background:#0b1220;padding:24px;color:#e5e7eb'>
    <div style='max-width:520px;margin:auto;background:#111827;border:1px solid #1f2937;border-radius:14px;padding:24px'>
        <h2 style='margin-top:0;color:#ffffff'>🔐 Réinitialisation du mot de passe</h2>
        <p>Votre code Ranita est :</p>
        <div style='margin:20px 0;padding:14px 18px;background:#22c55e;color:#06240f;
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
            channel = "email",
            message = "Si un compte existe, un code de réinitialisation a été envoyé."
        });
    }


    [HttpPost("client/reset-password")]
    public async Task<IActionResult> ClientResetPassword([FromBody] ClientResetPasswordDto dto)
    {
        var raw = (dto.Login ?? "").Trim();
        var code = (dto.Code ?? "").Trim();
        var newPassword = dto.NewPassword ?? "";
        var confirmPassword = dto.ConfirmPassword ?? "";

        if (string.IsNullOrWhiteSpace(raw))
            return BadRequest(new { ok = false, message = "Email ou téléphone obligatoire." });

        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { ok = false, message = "Code obligatoire." });

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
            return BadRequest(new { ok = false, message = "Mot de passe trop court." });

        if (newPassword != confirmPassword)
            return BadRequest(new { ok = false, message = "La confirmation du mot de passe est incorrecte." });

        var phone = NormalizePhone(raw);
        var email = NormalizeEmail(raw);

        Customer? customer = null;
        string loginValue = "";

        if (!string.IsNullOrWhiteSpace(phone))
        {
            customer = await _db.Customers.FirstOrDefaultAsync(x => x.Phone == phone && x.IsActive);
            if (customer != null) loginValue = phone;
        }

        if (customer == null && !string.IsNullOrWhiteSpace(email))
        {
            customer = await _db.Customers.FirstOrDefaultAsync(x => x.Email == email && x.IsActive);
            if (customer != null) loginValue = email;
        }

        if (customer == null)
            return BadRequest(new { ok = false, message = "Compte introuvable." });

        var reset = await _db.CustomerPasswordResets
            .Where(x => x.CustomerId == customer.Id
                        && x.LoginValue == loginValue
                        && x.ResetCode == code
                        && !x.IsUsed)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        if (reset == null)
            return BadRequest(new { ok = false, message = "Code invalide." });

        if (reset.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { ok = false, message = "Code expiré." });

        customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        reset.IsUsed = true;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            ok = true,
            message = "Mot de passe réinitialisé avec succès."
        });
    }
}