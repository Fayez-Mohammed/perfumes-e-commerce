namespace WallsShop.Repository;

public class ReviewResponseDto2
{
    public int Id { get; set; }
    public string? Comment { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; } // تم التغيير لاسم المنتج
    public DateTime ReviewDate { get; set; }
    public decimal Rate { get; set; }
}