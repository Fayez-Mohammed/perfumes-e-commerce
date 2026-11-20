using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using WallsShop.Entity;

namespace WallsShop.Properties.Entity;

[Index(nameof(PaymentReference))]
public class Order
{
    [Key]
    public int Id { get; set; }
    public string? UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual User? Customers { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    /// <summary>Pending | Confirmed | Canceled | AwaitingPayment (أوردر أونلاين لسه مدفعش)</summary>
    [MaxLength(30)]
    public string Status { get; set; } = "Pending";

    [MaxLength(1000)]

    public string? ShippingAddress { get; set; }
    public string? ReceiverPhone { get; set; }
    public string? ReceiverName { get; set; }
    [MaxLength(1000)]
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }

    // ===== Payment (Paymob) =====
    // الأوردرات القديمة هتبقى القيم دي فيها NULL، وبنعتبرها COD / Unpaid.

    /// <summary>COD | Card | Wallet</summary>
    [MaxLength(20)]
    public string? PaymentMethod { get; set; }

    /// <summary>Unpaid (COD) | Pending | Paid | Failed | Refunded | Voided</summary>
    [MaxLength(20)]
    public string? PaymentStatus { get; set; }

    /// <summary>الـ special_reference اللي بنبعته لـ Paymob، وبيرجع في الـ webhook كـ merchant_order_id.</summary>
    [MaxLength(100)]
    public string? PaymentReference { get; set; }

    /// <summary>رقم الأوردر عند Paymob (intention_order_id).</summary>
    public long? PaymobOrderId { get; set; }

    /// <summary>رقم العملية (transaction id) عند Paymob لما الدفع ينجح.</summary>
    public long? PaymobTransactionId { get; set; }

    public DateTime? PaidAt { get; set; }

    public virtual List<OrderDetails> OrderDetailsList { get; set; } = new();
}
