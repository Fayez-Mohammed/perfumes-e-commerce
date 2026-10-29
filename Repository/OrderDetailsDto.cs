
namespace WallsShop.Repository
{
    public class OrderDetailsDto
    {
        public int Id { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Status { get; set; }
        public string? Note { get; set; }
        public  List<OrderItemDto> Items { get; set; }
    }
}