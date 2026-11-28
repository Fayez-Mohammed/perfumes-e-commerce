namespace WallsShop.DTO;

/// <summary>Customer-facing summary and tracking details for a single order.</summary>
public class UserOrderDto
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "COD";
    public string PaymentStatus { get; set; } = "Unpaid";
    public string PaymentStatusLabel { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverPhone { get; set; }
    public string? ShippingAddress { get; set; }
    public string? Note { get; set; }
    public int ItemsCount { get; set; }
    public List<UserOrderItemDto> Items { get; set; } = new();
}

/// <summary>Line item inside a customer's order.</summary>
public class UserOrderItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public List<string> Images { get; set; } = new();
    public int? VariantId { get; set; }
    public string? Type { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}
