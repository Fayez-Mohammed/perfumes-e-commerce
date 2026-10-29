using System.Drawing;
using WallsShop.Entity;

namespace WallsShop.DTO;

public class ProductTranslationDto
{
    public int Id { get; set; }
    
   // public int ProductId { get; set; }
    
    public string Category { get; set; }
    public string CateogryValue { get; set; }

    public string SKU { get; set; }
    
    public decimal AverageRate { get; set; }
    
    public string Name { get; set; }
    
    public decimal Price { get; set; }
    
    public decimal PriceAfterDiscount { get; set; }
    
    public string ShortDescription { get; set; }
 

    public List<string> Descriptions { get; set; }
    public List<ProductVariantDto> Variants { get; set; }
    
    public List<ProductImageDto> Images { get; set; }

    // public List<string> Colors { get; set; }
    public List<ColorDto> Colors { get; set; }

    public bool IsInWishList { get; set; }
    public int PageNumber { get; internal set; }
   // public List<ProductVariantDto> Variants { get; set; }

}
public class ProductENARDto
{
    public int Id { get; set; }

    // public int ProductId { get; set; }
    public string nameEn { get; internal set; }
    public string? nameAr { get; internal set; }
    public string fullDescriptionAr { get; internal set; }
    public string shortDescriptionAr { get; internal set; }
    public string shortDescriptionEn { get; set; }


    public string fullDescriptionEn { get; set; }
    public List<ColorDto0> colors { get; internal set; }
   // public List<string> Colors { get; set; }

    public string Category { get; set; }
    public string ArabicCategory { get;  set; }

    public string categoryValue { get; set; }

    public string sku { get; set; }
    
    public decimal AverageRate { get; set; }
    
    //public string Name { get; set; }
    
    public decimal Price { get; set; }
    
    public decimal PriceAfterDiscount { get; set; }
    
   
    public List<ProductVariantENARDto> Variants { get; set; }
    
    public List<ProductImageWithIdDto> Images { get; set; }
    public List<availableSize> availableSizes { get; set; }= new List<availableSize>();
    public List<availableType> availableTypes { get; set; } = new List<availableType>();

    public bool IsInWishList { get; set; }
   // public int PageNumber { get; internal set; }

    // public List<ProductVariantDto> Variants { get; set; }

}
public class availableSize
{
    public string Id { get; set; }

    public string sizeEn { get; set; }
    public string sizeAr { get; set; }

} 
public class availableType
{
    public string Id { get; set; }

    public string typeEn { get; set; }
    public string typeAr { get; set; }

} 
