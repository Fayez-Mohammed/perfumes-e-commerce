namespace WallsShop.Repository
{
    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
      //  public int? VariantId { get; set; }
      public string? Type { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public List<string>? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}