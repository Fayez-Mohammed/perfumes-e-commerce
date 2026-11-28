using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using WallsShop.Context;
using WallsShop.DTO;
using WallsShop.Entity;
using WallsShop.Properties.Entity;
using WallsShop.Repository;
using WallsShop.Services;

namespace WallsShop.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrdersController : ControllerBase
{
    private const string Cod = "COD";
    private const string Card = "Card";
    private const string Wallet = "Wallet";

    private readonly WallShopContext _context;
    private readonly CartService _cartService;
    private readonly OrderRepository _orderRepo;
    private readonly IPaymobService _paymob;
    private readonly PaymobSettings _paymobSettings;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        WallShopContext context,
        CartService cartService,
        OrderRepository orderRepo,
        IPaymobService paymob,
        IOptions<PaymobSettings> paymobSettings,
        ILogger<OrdersController> logger)
    {
        _context = context;
        _cartService = cartService;
        _orderRepo = orderRepo;
        _paymob = paymob;
        _paymobSettings = paymobSettings.Value;
        _logger = logger;
    }

    /// <summary>Gets all orders placed by the currently authenticated user, including items and payment/delivery tracking info.</summary>
    /// <param name="LanguageCode">Language code used for localized labels ("ar" or "en", defaults to "ar").</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The list of the authenticated user's orders.</returns>
    [HttpGet("my-orders")]
    [ProducesResponseType(typeof(List<UserOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyOrders([FromQuery] string LanguageCode = "ar", CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var orders = await _orderRepo.GetUserOrdersAsync(userId, LanguageCode, ct);
        return Ok(orders);
    }

    /// <summary>Gets full details and item breakdown for a specific order belonging to the authenticated user.</summary>
    /// <param name="id">The identifier of the order.</param>
    /// <param name="LanguageCode">Language code used for localized labels ("ar" or "en", defaults to "ar").</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The order details if owned by the current user; otherwise 404 NotFound.</returns>
    [HttpGet("my-orders/{id:int}")]
    [ProducesResponseType(typeof(UserOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyOrderById(int id, [FromQuery] string LanguageCode = "ar", CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var order = await _orderRepo.GetUserOrderDetailsAsync(id, userId, LanguageCode, ct);
        if (order == null)
            return NotFound(new { message = LanguageCode == "ar" ? "الطلب غير موجود" : "Order not found" });

        return Ok(order);
    }

    /// <summary>Creates a new order for the authenticated user (cash on delivery or online payment via Paymob).</summary>
    /// <param name="dto">Order details supplied in the request body.</param>
    /// <param name="LanguageCode">Language code used for localized messages ("ar" for Arabic, otherwise English).</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>An order creation result including order id, payment method, requires payment flag, and checkout URL.</returns>
    [HttpPost("MakeOrder")]
    [ProducesResponseType(typeof(OrderCreateResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(object), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> MakeOrder([FromBody] CreateOrderDto dto, [FromQuery] string LanguageCode = "en", CancellationToken ct = default)
    {
        var ar = LanguageCode == "ar";

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized(ar ? "توكين المستخدم غير صالح" : "Invalid User Token");

        // ---------- طريقة الدفع ----------
        var method = NormalizeMethod(dto.PaymentMethod);
        if (method == null)
            return BadRequest(ar ? "طريقة الدفع غير صحيحة" : "Invalid payment method");

        var online = method != Cod;
        var integrationId = method == Card ? _paymobSettings.CardIntegrationId
                          : method == Wallet ? _paymobSettings.WalletIntegrationId
                          : 0;
        if (online && integrationId <= 0)
            return BadRequest(ar ? "طريقة الدفع دي مش متاحة حاليًا" : "This payment method is currently unavailable");

        // ---------- الكارت ----------
        var cartItems = await _cartService.GetCartItems(userId, "ar");
        if (cartItems == null || !cartItems.Any())
            return BadRequest(ar ? "عربة التسوق فارغة" : "Cart is empty");

        // الإجمالي بالقروش من سعر الوحدة (نفس الرقم هيتبعت لـ Paymob)
        var totalCents = cartItems.Sum(x => PaymobMoney.ToCents(x.UnitPrice) * x.Quantity);
        if (totalCents <= 0)
            return BadRequest(ar ? "إجمالي الطلب غير صحيح" : "Invalid order total");

        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            // الأونلاين بيفضل "في انتظار الدفع" لحد ما الـ webhook يأكد الدفع
            Status = online ? "AwaitingPayment" : "Pending",
            TotalAmount = totalCents / 100m,
            Note = dto.Note,

            ReceiverName = dto.FullName,
            ReceiverPhone = dto.PhoneNumber,
            ShippingAddress = dto.Address,

            PaymentMethod = method,
            PaymentStatus = online ? "Pending" : "Unpaid",
            PaymentReference = online ? $"WS-{Guid.NewGuid():N}" : null,

            OrderDetailsList = cartItems.Select(item => new OrderDetails
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Price = item.UnitPrice,
                Quantity = item.Quantity,
                VariantId = item.VariantId != 0 ? item.VariantId : null,
                Color = item.Color
            }).ToList()
        };

        try
        {
            await _context.Orders.AddAsync(order, ct);
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order for user {UserId}", userId);
            return StatusCode(500, ar ? "حصل خطأ أثناء إنشاء الطلب" : "Error creating order");
        }

        // ---------- الدفع عند الاستلام: زي الأول بالظبط ----------
        if (!online)
        {
            _cartService.ClearCart(userId);
            return Ok(new
            {
                Message = ar ? "تم إنشاء الطلب بنجاح" : "Order created successfully",
                OrderId = order.Id,
                PaymentMethod = method,
                RequiresPayment = false,
                CheckoutUrl = (string?)null
            });
        }

        // ---------- الدفع الأونلاين: نفتح Intention عند Paymob ----------
        try
        {
            var items = cartItems
                .Select(i => new PaymobItem(i.ProductName ?? "Item", PaymobMoney.ToCents(i.UnitPrice), i.Quantity))
                .ToList();

            var (first, last) = SplitName(dto.FullName);
            var billing = new PaymobBilling(
                first,
                last,
                User.FindFirstValue(ClaimTypes.Email) ?? "customer@wallsshop-furniture.com",
                NormalizePhone(dto.PhoneNumber),
                dto.Address);

            var result = await _paymob.CreateIntentionAsync(
                new PaymobIntentionRequest(totalCents, integrationId, order.PaymentReference!, order.Id, items, billing), ct);

            order.PaymobOrderId = result.IntentionOrderId;
            await _context.SaveChangesAsync(ct);

            // الكارت بيتمسح بعد نجاح الدفع (في الـ webhook) مش دلوقتي
            return Ok(new
            {
                Message = ar ? "تم إنشاء الطلب، حوّلناك لصفحة الدفع" : "Order created, redirecting to payment",
                OrderId = order.Id,
                PaymentMethod = method,
                RequiresPayment = true,
                CheckoutUrl = (string?)result.CheckoutUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paymob intention failed for order {OrderId}", order.Id);

            // نقفل الأوردر عشان ميفضلش معلّق، والكارت لسه موجود فالعميل يقدر يجرب تاني
            try
            {
                order.Status = "Canceled";
                order.PaymentStatus = "Failed";
                await _context.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Could not mark order {OrderId} as failed", order.Id);
            }

            return StatusCode(502, ar
                ? "تعذر فتح صفحة الدفع الآن، حاول مرة أخرى أو اختر الدفع عند الاستلام"
                : "Could not start the payment, please try again or choose cash on delivery");
        }
    }

    private static string? NormalizeMethod(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Cod;
        var v = input.Trim();
        if (v.Equals(Cod, StringComparison.OrdinalIgnoreCase)) return Cod;
        if (v.Equals(Card, StringComparison.OrdinalIgnoreCase)) return Card;
        if (v.Equals(Wallet, StringComparison.OrdinalIgnoreCase)) return Wallet;
        return null;
    }

    private static (string First, string Last) SplitName(string? fullName)
    {
        var parts = (fullName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return ("NA", "NA");
        if (parts.Length == 1) return (parts[0], "NA");
        return (parts[0], string.Join(' ', parts.Skip(1)));
    }

    /// <summary>01012345678 → +201012345678 (الصيغة اللي Paymob بتفضلها).</summary>
    private static string NormalizePhone(string? phone)
    {
        var p = new string((phone ?? "").Where(c => char.IsDigit(c) || c == '+').ToArray());
        if (p.StartsWith("01") && p.Length == 11) return "+2" + p;
        if (p.StartsWith("201") && p.Length == 12) return "+" + p;
        return p;
    }
}
