using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using WallsShop.Context;
using WallsShop.Services;

namespace WallsShop.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly WallShopContext _context;
    private readonly CartService _cartService;
    private readonly PaymobSettings _settings;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        WallShopContext context,
        CartService cartService,
        IOptions<PaymobSettings> settings,
        ILogger<PaymentsController> logger)
    {
        _context = context;
        _cartService = cartService;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Paymob بتنده الـ endpoint ده بعد كل عملية دفع (Transaction Processed Callback).
    /// مفيش JWT هنا، الحماية كلها بالـ HMAC.
    /// لازم يرجع 200 لأي callback سليم (حتى لو مش هنعمل بيه حاجة)، وإلا Paymob هتفضل تعيد المحاولة.
    /// </summary>
    /// <param name="hmac">HMAC signature sent by Paymob as a query string parameter, used to verify the payload.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>200 when the callback is valid or ignored, 400 for a malformed payload, 401 for an invalid HMAC signature.</returns>
    [AllowAnonymous]
    [HttpPost("paymob-webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PaymobWebhook([FromQuery] string? hmac, CancellationToken ct)
    {
        JsonDocument doc;
        try
        {
            doc = await JsonDocument.ParseAsync(Request.Body, cancellationToken: ct);
        }
        catch (JsonException)
        {
            return BadRequest();
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return BadRequest();

            // Paymob بتبعت كمان callback للـ card token على نفس الرابط (نوعه TOKEN)، منعمل بيه حاجة
            if (root.TryGetProperty("type", out var typeEl) &&
                typeEl.ValueKind == JsonValueKind.String &&
                !string.Equals(typeEl.GetString(), "TRANSACTION", StringComparison.OrdinalIgnoreCase))
            {
                return Ok();
            }

            if (!root.TryGetProperty("obj", out var obj) || obj.ValueKind != JsonValueKind.Object)
                return BadRequest();

            // ===== 1) التحقق من التوقيع =====
            if (!PaymobHmac.IsValid(obj, hmac ?? "", _settings.HmacSecret))
            {
                _logger.LogWarning(
                    "Paymob webhook rejected: invalid HMAC (txn id {TxnId}, merchant ref {Ref}). Check Paymob:HmacSecret.",
                    PaymobHmac.ReadValue(obj, "id"),
                    PaymobHmac.ReadValue(obj, "order.merchant_order_id"));
                return Unauthorized();
            }

            // ===== 2) نلاقي الأوردر من الـ reference بتاعنا =====
            var reference = PaymobHmac.ReadValue(obj, "order.merchant_order_id");
            if (string.IsNullOrWhiteSpace(reference))
            {
                _logger.LogWarning("Paymob webhook without merchant_order_id (txn {TxnId})", PaymobHmac.ReadValue(obj, "id"));
                return Ok();
            }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.PaymentReference == reference, ct);
            if (order == null)
            {
                _logger.LogError("Paymob webhook for unknown reference {Ref} (txn {TxnId})", reference, PaymobHmac.ReadValue(obj, "id"));
                return Ok();
            }

            // ===== 3) نتأكد إن المبلغ والعملة مطابقين =====
            var expectedCents = PaymobMoney.ToCents(order.TotalAmount);
            var amountOk = decimal.TryParse(PaymobHmac.ReadValue(obj, "amount_cents"),
                               NumberStyles.Any, CultureInfo.InvariantCulture, out var paidCents)
                           && paidCents == expectedCents;
            var currencyOk = string.Equals(PaymobHmac.ReadValue(obj, "currency"), _settings.Currency, StringComparison.OrdinalIgnoreCase);

            if (!amountOk || !currencyOk)
            {
                _logger.LogError(
                    "Paymob webhook mismatch for order {OrderId}: expected {Expected} {Cur}, got {Got} {GotCur}. Needs manual review.",
                    order.Id, expectedCents, _settings.Currency,
                    PaymobHmac.ReadValue(obj, "amount_cents"), PaymobHmac.ReadValue(obj, "currency"));
                return Ok();
            }

            // ===== 4) نحدد حالة العملية =====
            var success = PaymobHmac.ReadValue(obj, "success") == "true";
            var pending = PaymobHmac.ReadValue(obj, "pending") == "true";
            var refunded = PaymobHmac.ReadValue(obj, "is_refunded") == "true";
            var voided = PaymobHmac.ReadValue(obj, "is_voided") == "true";
            long.TryParse(PaymobHmac.ReadValue(obj, "id"), out var txnId);

            if (pending) return Ok(); // لسه بتتعالج، هييجي callback تاني

            var alreadyPaid = order.PaymentStatus == "Paid";

            if (refunded || voided)
            {
                // الاسترجاع/الإلغاء بيتسجل بس (والأدمن يتابع الأوردر بنفسه)
                order.PaymentStatus = refunded ? "Refunded" : "Voided";
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Order {OrderId} payment marked {Status}", order.Id, order.PaymentStatus);
                return Ok();
            }

            if (success)
            {
                if (alreadyPaid) return Ok(); // callback متكرر، نتجاهله

                order.PaymentStatus = "Paid";
                order.PaidAt = DateTime.UtcNow;
                order.PaymobTransactionId = txnId == 0 ? null : txnId;

                if (order.Status == "AwaitingPayment")
                    order.Status = "Pending"; // دلوقتي مستني تأكيد الأدمن
                else if (order.Status == "Canceled")
                    _logger.LogWarning("Order {OrderId} was Canceled but got paid (txn {TxnId}). Refund may be needed.", order.Id, txnId);

                await _context.SaveChangesAsync(ct);

                if (!string.IsNullOrEmpty(order.UserId))
                    _cartService.ClearCart(order.UserId);

                _logger.LogInformation("Order {OrderId} paid (txn {TxnId})", order.Id, txnId);
                return Ok();
            }

            // فشلت المحاولة: العميل ممكن يعيد المحاولة في نفس صفحة الدفع، فمنقفلش الأوردر
            if (!alreadyPaid)
            {
                order.PaymentStatus = "Failed";
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Order {OrderId} payment attempt failed (txn {TxnId})", order.Id, txnId);
            }

            return Ok();
        }
    }

    /// <summary>
    /// Paymob بترجّع العميل هنا بعد الدفع (Transaction Response Callback).
    /// إحنا مش بنعتمد على الـ query params في تغيير حالة الأوردر،
    /// بنستخدمهم بس للوصول لرقم الأوردر عندنا وتحويل العميل لصفحة النتيجة في الفرونت.
    /// الحالة الحقيقية بتيجي من الـ webhook أو من /status/{orderId}.
    /// </summary>
    /// <param name="order">Paymob order id, used to look up the local order.</param>
    /// <param name="id">Paymob transaction id, used as a fallback lookup key for the local order.</param>
    /// <param name="merchantOrderId">Our payment reference, used as a further fallback lookup key for the local order.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>A 302 redirect to the front-end payment result page (or the site root when the order cannot be found).</returns>
    [AllowAnonymous]
    [HttpGet("response-callback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> ResponseCallback(
        [FromQuery] long? order,
        [FromQuery] long? id,
        [FromQuery(Name = "merchant_order_id")] string? merchantOrderId,
        CancellationToken ct)
    {
        int? localOrderId = null;

        if (order is > 0)
        {
            localOrderId = await _context.Orders
                .Where(o => o.PaymobOrderId == order)
                .Select(o => (int?)o.Id)
                .FirstOrDefaultAsync(ct);
        }

        if (localOrderId == null && id is > 0)
        {
            localOrderId = await _context.Orders
                .Where(o => o.PaymobTransactionId == id)
                .Select(o => (int?)o.Id)
                .FirstOrDefaultAsync(ct);
        }

        if (localOrderId == null && !string.IsNullOrWhiteSpace(merchantOrderId))
        {
            localOrderId = await _context.Orders
                .Where(o => o.PaymentReference == merchantOrderId)
                .Select(o => (int?)o.Id)
                .FirstOrDefaultAsync(ct);
        }

        if (localOrderId == null)
        {
            _logger.LogWarning(
                "Paymob response callback: no local order found (Paymob order {PaymobOrderId}, txn {TxnId}, ref {Ref}).",
                order, id, merchantOrderId);

            var home = new Uri(BuildResultUrl(0));
            return Redirect(new Uri(home, "/").ToString());
        }

        return Redirect(BuildResultUrl(localOrderId.Value));
    }

    private string BuildResultUrl(int orderId)
    {
        var template = string.IsNullOrWhiteSpace(_settings.RedirectionUrlTemplate)
            ? "https://mariam-perfumes.vercel.app/payment-result/{orderId}"
            : _settings.RedirectionUrlTemplate;

        return template.Replace("{orderId}", orderId.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// الفرونت بيسأل هنا بعد ما العميل يرجع من Paymob.
    /// متعتمدش على الـ query params اللي بترجع في الـ redirect: هي مش موثوقة، وممكن توصل قبل الـ webhook (اسأل كام مرة بفاصل ثانيتين).
    /// </summary>
    /// <param name="orderId">Identifier of the order whose payment status is requested.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>An anonymous object with the order status, payment method, payment status, total amount and paid date; 404 when no matching order belongs to the user.</returns>
    [Authorize]
    [HttpGet("status/{orderId:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentStatus(int orderId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var order = await _context.Orders
            .Where(o => o.Id == orderId && o.UserId == userId)
            .Select(o => new
            {
                OrderId = o.Id,
                o.Status,
                PaymentMethod = o.PaymentMethod ?? "COD",
                PaymentStatus = o.PaymentStatus ?? "Unpaid",
                o.TotalAmount,
                o.PaidAt
            })
            .FirstOrDefaultAsync(ct);

        return order == null ? NotFound() : Ok(order);
    }
}
