using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EcommerceApi.Service;

public class WhatsAppService
{
    private readonly IConfiguration _cfg;
    private readonly HttpClient _http;

    public WhatsAppService(IConfiguration cfg, HttpClient http)
    {
        _cfg = cfg;
        _http = http;
    }

    public async Task SendHelloWorldTemplateAsync(string toPhone)
    {
        var token = _cfg["WhatsApp:AccessToken"];
        var phoneNumberId = _cfg["WhatsApp:PhoneNumberId"];
        var apiVersion = _cfg["WhatsApp:ApiVersion"] ?? "v22.0";

        if (string.IsNullOrWhiteSpace(token))
            throw new Exception("WhatsApp:AccessToken manquant.");

        if (string.IsNullOrWhiteSpace(phoneNumberId))
            throw new Exception("WhatsApp:PhoneNumberId manquant.");

        if (string.IsNullOrWhiteSpace(toPhone))
            throw new Exception("Numéro destinataire vide.");

        var to = NormalizePhoneForWhatsApp(toPhone);
        var url = $"https://graph.facebook.com/{apiVersion}/{phoneNumberId}/messages";

        var payload = new
        {
            messaging_product = "whatsapp",
            to = to,
            type = "template",
            template = new
            {
                name = "hello_world",
                language = new
                {
                    code = "en_US"
                }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        var res = await _http.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();

        Console.WriteLine("WHATSAPP TO => " + to);
        Console.WriteLine("WHATSAPP RESPONSE => " + body);

        if (!res.IsSuccessStatusCode)
            throw new Exception($"Erreur WhatsApp HTTP {(int)res.StatusCode}: {body}");
    }

    private string NormalizePhoneForWhatsApp(string phone)
    {
        phone = (phone ?? "").Trim();

        phone = Regex.Replace(phone, @"[^\d+]", "");

        if (phone.StartsWith("+"))
            phone = phone[1..];

        if (phone.StartsWith("00"))
            phone = phone[2..];

        var defaultCountryCode = _cfg["WhatsApp:DefaultCountryCode"] ?? "225";

        if (!phone.StartsWith(defaultCountryCode))
            phone = defaultCountryCode + phone.TrimStart('0');

        if (!Regex.IsMatch(phone, @"^\d{8,15}$"))
            throw new Exception("Numéro WhatsApp invalide après normalisation.");

        return phone;
    }

    public async Task SendResetCodeAsync(string toPhone, string code)
    {
        var token = _cfg["WhatsApp:AccessToken"];
        var phoneNumberId = _cfg["WhatsApp:PhoneNumberId"];
        var apiVersion = _cfg["WhatsApp:ApiVersion"] ?? "v22.0";

        if (string.IsNullOrWhiteSpace(token))
            throw new Exception("WhatsApp:AccessToken manquant.");

        if (string.IsNullOrWhiteSpace(phoneNumberId))
            throw new Exception("WhatsApp:PhoneNumberId manquant.");

        if (string.IsNullOrWhiteSpace(toPhone))
            throw new Exception("Numéro destinataire vide.");

        if (string.IsNullOrWhiteSpace(code))
            throw new Exception("Code de réinitialisation vide.");

        var to = NormalizePhoneForWhatsApp(toPhone);
        var url = $"https://graph.facebook.com/{apiVersion}/{phoneNumberId}/messages";

        var payload = new
        {
            messaging_product = "whatsapp",
            to = to,
            type = "template",
            template = new
            {
                name = "reset_password_code",
                language = new
                {
                    code = "fr_FR"
                },
                components = new object[]
                {
                new
                {
                    type = "body",
                    parameters = new object[]
                    {
                        new
                        {
                            type = "text",
                            text = code
                        }
                    }
                }
                }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        var res = await _http.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();

        Console.WriteLine("WHATSAPP RESET TO => " + to);
        Console.WriteLine("WHATSAPP RESET RESPONSE => " + body);

        if (!res.IsSuccessStatusCode)
            throw new Exception($"Erreur WhatsApp HTTP {(int)res.StatusCode}: {body}");
    }
}