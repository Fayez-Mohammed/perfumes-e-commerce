namespace WallsShop.DTO;

public class OrderDashboardDto
    {
        public int Id { get; set; }

        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }
        public string? OrderDate { get; set; } 

        public int ItemsCount { get; set; } 
        public decimal TotalPrice { get; set; }
        public string? Status { get; set; }
         public string? Note { get; set; }
}
