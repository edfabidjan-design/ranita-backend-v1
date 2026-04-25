using System.Net;
using System.Net.Mail;

namespace EcommerceApi.Service;

public class EmailService
{
    private readonly IConfiguration _cfg;

    public EmailService(IConfiguration cfg)
    {
        _cfg = cfg;
    }

    public async Task SendAsync(string toEmail, string subject, string body)
    {
        var host = _cfg["Smtp:Host"] ?? "smtp.gmail.com";
        var port = int.TryParse(_cfg["Smtp:Port"], out var p) ? p : 587;

        var user = _cfg["Smtp:User"];
        var pass = _cfg["Smtp:Password"];

        var fromName = _cfg["Smtp:FromName"] ?? "Ranita";
        var fromEmail = _cfg["Smtp:FromEmail"] ?? user;

        if (string.IsNullOrWhiteSpace(user))
            throw new Exception("Smtp:User manquant dans appsettings.json");

        if (string.IsNullOrWhiteSpace(pass))
            throw new Exception("Smtp:Password manquant dans appsettings.json");

        if (string.IsNullOrWhiteSpace(toEmail))
            throw new Exception("Destinataire email vide.");

        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(user, pass),
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(fromEmail!, fromName),
            Subject = subject ?? "",
            Body = body ?? "",
            IsBodyHtml = true
        };

        mail.To.Add(toEmail.Trim());

        try
        {
            await smtp.SendMailAsync(mail);
        }
        catch (SmtpException ex)
        {
            throw new Exception($"SMTP ERROR ({ex.StatusCode}) : {ex.Message}", ex);
        }
    }

    public string GetBoutiqueEmail()
    {
        return _cfg["Smtp:ToEmail"] ?? "";
    }

    public async Task SendVendorTermsEmailAsync(string email, string name, decimal commission)
    {
        var subject = "Ranita - Contrat vendeur à signer";

        var html = $@"
<h2>Bonjour {name},</h2>

<p>
Votre demande de création de boutique sur <b>Ranita</b> a bien été enregistrée.
</p>

<p>
Vous trouverez en pièce jointe votre <b>contrat vendeur</b>.
</p>

<p>
👉 Merci de :
</p>

<ul>
<li>Lire attentivement le contrat</li>
<li>Signer le document</li>
<li>Le renvoyer par réponse à cet email</li>
</ul>

<p>
<b>Commission Ranita :</b> {commission}% par vente
</p>

<hr/>

<p style='color:#9ca3af;font-size:13px'>
⚠️ Votre boutique sera activée uniquement après réception et validation du contrat signé.
</p>

<p>
Cordialement,<br/>
<b>Equipe Ranita</b>
</p>
";

        await SendAsync(email, subject, html);
    }


    public async Task SendWithAttachmentAsync(
    string toEmail,
    string subject,
    string body,
    byte[] fileBytes,
    string fileName)
    {
        var host = _cfg["Smtp:Host"] ?? "smtp.gmail.com";
        var port = int.TryParse(_cfg["Smtp:Port"], out var p) ? p : 587;

        var user = _cfg["Smtp:User"];
        var pass = _cfg["Smtp:Password"];
        var fromName = _cfg["Smtp:FromName"] ?? "Ranita";
        var fromEmail = _cfg["Smtp:FromEmail"] ?? user;

        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(user, pass),
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(fromEmail!, fromName),
            Subject = subject ?? "",
            Body = body ?? "",
            IsBodyHtml = true
        };

        mail.To.Add(toEmail.Trim());

        using var stream = new MemoryStream(fileBytes);
        mail.Attachments.Add(new Attachment(stream, fileName, "application/pdf"));

        await smtp.SendMailAsync(mail);
    }
}