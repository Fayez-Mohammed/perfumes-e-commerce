using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WallsShop.Services;

/// <summary>
/// بيتحقق من توقيع الـ Transaction Processed Callback بتاع Paymob.
/// الطريقة: نجمع قيم 20 حقل من obj بالترتيب الأبجدي، ونعمل HMAC-SHA512 بالـ HMAC Secret.
/// </summary>
public static class PaymobHmac
{
    // الترتيب ده حسب docs بتاعة Paymob (lexicographical). المسارات نسبية لـ obj.
    private static readonly string[] Fields =
    {
        "amount_cents",
        "created_at",
        "currency",
        "error_occured",
        "has_parent_transaction",
        "id",
        "integration_id",
        "is_3d_secure",
        "is_auth",
        "is_capture",
        "is_refunded",
        "is_standalone_payment",
        "is_voided",
        "order.id",
        "owner",
        "pending",
        "source_data.pan",
        "source_data.sub_type",
        "source_data.type",
        "success"
    };

    /// <summary>بيقرا قيمة من JSON بمسار زي "order.id". القيم الناقصة/null بترجع نص فاضي، والـ bool بتترجع true/false صغيرة.</summary>
    public static string ReadValue(JsonElement obj, string path)
    {
        var current = obj;
        foreach (var segment in path.Split('.'))
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out var next))
                return string.Empty;
            current = next;
        }

        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString() ?? string.Empty,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => current.GetRawText(),
            _ => string.Empty
        };
    }

    public static string BuildString(JsonElement obj) =>
        string.Concat(Fields.Select(f => ReadValue(obj, f)));

    public static string Compute(JsonElement obj, string secret)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(BuildString(obj)));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool IsValid(JsonElement obj, string receivedHmac, string secret)
    {
        // لو الـ secret مش متظبط نرفض كل حاجة (مش نقبل توقيع بمفتاح فاضي)
        if (string.IsNullOrEmpty(secret) || string.IsNullOrWhiteSpace(receivedHmac))
            return false;

        var expected = Encoding.UTF8.GetBytes(Compute(obj, secret));
        var received = Encoding.UTF8.GetBytes(receivedHmac.Trim().ToLowerInvariant());
        return CryptographicOperations.FixedTimeEquals(expected, received);
    }
}
