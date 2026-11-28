namespace WallsShop.DTO;

public class SingleReviewDto
{
    public int Id { get; set; } 
    public string UserName { get; set; }
    
    public string Comment { get; set; }
    
    public DateTime Date { get; set; }
    
    public bool CanBeDeleted { get; set; }
    
    public decimal Rate { get; set; }
}

/// <summary>Publicly visible review summary without sensitive user contact information.</summary>
public class PublicReviewDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal ProductPrice { get; set; }
    public decimal ProductPriceAfterDiscount { get; set; }
    public string? ProductImageUrl { get; set; }
    public DateTime ReviewDate { get; set; }
    public decimal Rate { get; set; }
}