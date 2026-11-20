using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace WallsShop.Services;

public class PaymobSettings
{
    public string BaseUrl { get; set; } = "https://accept.paymob.com";
    public string SecretKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string HmacSecret { get; set; } = string.Empty;

    /// <summary>Integration ID بتاع الكارت (Online Card).</summary>
    public int CardIntegrationId { get; set; }

    /// <summary>Integration ID بتاع المحفظة (Mobile Wallet - فودافون كاش). 0 = مقفول.</summary>
    public int WalletIntegrationId { get; set; }

    public string Currency { get; set; } = "EGP";

    /// <summary>رابط الـ API العام HTTPS (بدون / في الآخر). بيتبني منه notification_url.</summary>
    public string PublicApiUrl { get; set; } = string.Empty;

    /// <summary>رابط صفحة النتيجة في الفرونت. {orderId} بيتستبدل برقم الأوردر.</summary>
    public string RedirectionUrlTemplate { get; set; } = "https://mariam-perfumes.vercel.app/payment-result/{orderId}";
}

public static class PaymobMoney
{
    /// <summary>جنيه → قرش. بتتساوى مع اللي Paymob مستنيه في amount.</summary>
    public static long ToCents(decimal amount) =>
        (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
}

public record PaymobItem(string Name, long UnitAmountCents, int Quantity);

public record PaymobBilling(string FirstName, string LastName, string Email, string Phone, string Street);

public record PaymobIntentionRequest(
    long AmountCents,
    int IntegrationId,
    string Reference,
    int OrderId,
    IReadOnlyList<PaymobItem> Items,
    PaymobBilling Billing);

public record PaymobIntentionResult(string ClientSecret, long? IntentionOrderId, string CheckoutUrl);

public class PaymobException : Exception
{
    public PaymobException(string message, Exception? inner = null) : base(message, inner) { }
}

public interface IPaymobService
{
    Task<PaymobIntentionResult> CreateIntentionAsync(PaymobIntentionRequest request, CancellationToken ct = default);
}

public class PaymobService : IPaymobService
{
    private readonly HttpClient _http;
    private readonly PaymobSettings _settings;

    public PaymobService(HttpClient http, IOptions<PaymobSettings> settings)
    {
        _http = http;
        _settings = settings.Value;
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<PaymobIntentionResult> CreateIntentionAsync(PaymobIntentionRequest r, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SecretKey) || string.IsNullOrWhiteSpace(_settings.PublicKey))
            throw new PaymobException("Paymob keys are not configured (Paymob:SecretKey / Paymob:PublicKey).");

        if (!_settings.PublicApiUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new PaymobException("Paymob:PublicApiUrl must be a public HTTPS url (used for the webhook).");

        // Paymob بتتحقق إن مجموع (سعر الصنف × الكمية) = amount
        var itemsTotal = r.Items.Sum(i => i.UnitAmountCents * i.Quantity);
        if (itemsTotal != r.AmountCents)
            throw new PaymobException($"Items total ({itemsTotal}) does not match amount ({r.AmountCents}).");

        var payload = new
        {
            amount = r.AmountCents,
            currency = _settings.Currency,
            payment_methods = new[] { r.IntegrationId },
            items = r.Items.Select(i => new
            {
                name = Truncate(i.Name, 50),
                amount = i.UnitAmountCents,
                description = Truncate(i.Name, 50),
                quantity = i.Quantity
            }).ToArray(),
            billing_data = new
            {
                first_name = OrNa(r.Billing.FirstName),
                last_name = OrNa(r.Billing.LastName),
                email = OrNa(r.Billing.Email),
                phone_number = OrNa(r.Billing.Phone),
                street = OrNa(r.Billing.Street),
                country = "EG",
                city = "NA",
                state = "NA",
                apartment = "NA",
                floor = "NA",
                building = "NA",
                postal_code = "NA",
                shipping_method = "NA"
            },
            special_reference = r.Reference,
            notification_url = $"{_settings.PublicApiUrl.TrimEnd('/')}/api/Payments/paymob-webhook",
            redirection_url = _settings.RedirectionUrlTemplate.Replace("{orderId}", r.OrderId.ToString())
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl.TrimEnd('/')}/v1/intention/")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Token", _settings.SecretKey);

        HttpResponseMessage response;
        string body;
        try
        {
            response = await _http.SendAsync(message, ct);
            body = await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new PaymobException("Could not reach Paymob.", ex);
        }

        if (!response.IsSuccessStatusCode)
            throw new PaymobException($"Paymob returned {(int)response.StatusCode}: {body}");

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var clientSecret = root.GetProperty("client_secret").GetString();
            if (string.IsNullOrWhiteSpace(clientSecret))
                throw new PaymobException("Paymob response has no client_secret.");

            long? intentionOrderId = null;
            if (root.TryGetProperty("intention_order_id", out var oid) && oid.ValueKind == JsonValueKind.Number)
                intentionOrderId = oid.GetInt64();

            var url = $"{_settings.BaseUrl.TrimEnd('/')}/unifiedcheckout/" +
                      $"?publicKey={Uri.EscapeDataString(_settings.PublicKey)}" +
                      $"&clientSecret={Uri.EscapeDataString(clientSecret)}";

            return new PaymobIntentionResult(clientSecret, intentionOrderId, url);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new PaymobException($"Unexpected Paymob response: {body}", ex);
        }
    }

    private static string OrNa(string? v) => string.IsNullOrWhiteSpace(v) ? "NA" : v.Trim();

    private static string Truncate(string? v, int max)
    {
        var s = string.IsNullOrWhiteSpace(v) ? "Item" : v.Trim();
        return s.Length <= max ? s : s[..max];
    }
}
