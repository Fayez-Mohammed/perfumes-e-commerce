namespace WallsShop.DTO;

/// <summary>Authentication result containing user email and JWT bearer token.</summary>
public class AuthResponseDto
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

/// <summary>Current authenticated user information including role memberships.</summary>
public class CurrentUserDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public List<string> Role { get; set; } = new();
}

/// <summary>Response model after verifying OTP, including password reset token.</summary>
public class VerifyOtpResponseDto
{
    public string Message { get; set; } = string.Empty;
    public string ResetToken { get; set; } = string.Empty;
}

/// <summary>Result returned after creating an order (COD or Paymob online payment).</summary>
public class OrderCreateResultDto
{
    public string Message { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public bool RequiresPayment { get; set; }
    public string? CheckoutUrl { get; set; }
}

/// <summary>Payment verification status details for an order.</summary>
public class PaymentStatusResponseDto
{
    public int OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime? PaidAt { get; set; }
}

/// <summary>Standard text message response.</summary>
public class MessageResponseDto
{
    public string Message { get; set; } = string.Empty;
}

/// <summary>Standard item count response.</summary>
public class CountResponseDto
{
    public int Count { get; set; }
}
