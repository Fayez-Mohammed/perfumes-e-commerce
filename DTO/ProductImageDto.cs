using Microsoft.AspNetCore.Components.Forms;

namespace WallsShop.DTO;

public class ProductImageDto
{
    public string Path { get; set; }
    
}
public class ProductImageWithIdDto
{
    public string Path { get; set; }
    public int Id { get; set; }
}