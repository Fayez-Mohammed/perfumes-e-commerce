using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
namespace WallsShop.DTO;
/// <summary>
/// Request wrapper للـ Update Product
/// </summary>
public class ProductUpdateRequest
{
    public int Id { get; set; }  
    public string Product { get; set; }   
    public List<IFormFile>? NewImages { get; set; }
    public List<int>? DeleteImageIds { get; set; }  
}

/// <summary>
/// DTO لتعديل منتج
/// </summary>
public class ProductUpdateDto
{
    // معلومات أساسية
    public string NameAr { get; set; }
    public string NameEn { get; set; }
    public string ShortDescriptionAr { get; set; }
    public string ShortDescriptionEn { get; set; }
    public string FullDescriptionAr { get; set; }
    public string FullDescriptionEn { get; set; }
    public string CategoryValue { get; set; }
    public string SKU { get; set; }

    // السعر الأساسي
    public decimal Price { get; set; }
    public decimal PriceAfterDiscount { get; set; }

    // الألوان
    public List<ColorDto0>? Colors { get; set; }

    public List<VariantDto0>? Variants { get; set; }
}


public class ProductAddRequest
{
    public string Product { get; set; }   // JSON
   public List<IFormFile>? Images { get; set; }
}

/// <summary>
/// DTO لإضافة منتج - يتوافق مع JSON من Frontend
/// </summary>
public class ProductAddDto0
{
    // معلومات أساسية
    public string NameAr { get; set; }
    public string NameEn { get; set; }
    public string ShortDescriptionAr { get; set; }
    public string ShortDescriptionEn { get; set; }
    public string FullDescriptionAr { get; set; }
    public string FullDescriptionEn { get; set; }
    public string CategoryValue { get; set; }
    public string SKU { get; set; }

    // السعر الأساسي (0 إذا كان في variants)
    public decimal Price { get; set; }
    public decimal PriceAfterDiscount { get; set; }
    // الألوان

    public List<ColorDto0>? Colors { get; set; }



    // الـ Variants بالأسعار

    public List<VariantDto0>? Variants { get; set; }

    // الصور

    // public List<IFormFile>? Images { get; set; }
}

public class ColorDto0
{
    public string NameAr { get; set; }
    public string NameEn { get; set; }
}

public class SizeDto0
{
    public string SizeAr { get; set; }
    public string SizeEn { get; set; }
}

public class TypeDto0
{
    public string TypeAr { get; set; }
    public string TypeEn { get; set; }
}

public class VariantDto0
{
    public SizeDto0 Size { get; set; }
    public TypeDto0 Type { get; set; }
    public decimal Price { get; set; }
    public decimal PriceAfterDiscount { get; set; }
}




////public class ProductAddDtoaren
////{
////    الأسماء
////    public string NameAR { get; set; }
////    public string NameEN { get; set; }

////    الأوصاف
////    public string DescriptionAR { get; set; }
////    public string DescriptionEN { get; set; }
////    public string ShortDescriptionAr { get; set; }
////    public string ShortDescriptionEn { get; set; }
////    public string FullDescriptionAr { get; set; }
////    public string FullDescriptionEn { get; set; }
////    معلومات أساسية
////    public string SKU { get; set; }
////    public decimal Price { get; set; }
////    public decimal? PriceBeforeDiscount { get; set; }
////    public string CategoryValue { get; set; }

////    الأحجام(JSON string أو array بسيط)
////    public List<string> SizesAR { get; set; }
////    public List<string> SizesEN { get; set; }

////    الأنواع
////    public List<string> TypesAR { get; set; }
////    public List<string> TypesEN { get; set; }

////    الألوان
////    public List<string> ColorsAR { get; set; }
////    public List<string> ColorsEN { get; set; }

////    الصور
////    public List<IFormFile>? ImageFiles { get; set; }
////    public List<string>? ImageUrls { get; set; }
////}


//public class ProductAddDto11
//{
//    // الأسماء
//    public string NameAR { get; set; }
//    public string NameEN { get; set; }

//    // الأوصاف
//    public string DescriptionAR { get; set; }
//    public string DescriptionEN { get; set; }

//    // معلومات أساسية
//    public string SKU { get; set; }
//    public decimal Price { get; set; }
//    public decimal? PriceBeforeDiscount { get; set; }
//    public string CategoryValue { get; set; }

//    // الأحجام (JSON string أو array بسيط)
//    public List<string> SizesAR { get; set; }
//    public List<string> SizesEN { get; set; }

//    // الأنواع
//    public List<string> TypesAR { get; set; }
//    public List<string> TypesEN { get; set; }

//    // الألوان
//    public List<string> ColorsAR { get; set; }
//    public List<string> ColorsEN { get; set; }

//    // الصور
//    public List<IFormFile>? ImageFiles { get; set; }
//    public List<string>? ImageUrls { get; set; }
//}
//public class ProductAddDto22
//{
//    public string NameAR { get; set; }
//    public string NameEN { get; set; }

//    public string FullDescriptionAR { get; set; }
//    public string DescriptionAR { get; set; }
//    public string DescriptionEN { get; set; }

//    public string SKU { get; set; }
//    public decimal Price { get; set; }
//    public decimal? PriceBeforeDiscount { get; set; }
//    public string CategoryValue { get; set; }

//    public List<string> Sizes { get; set; }

//    public List<string> Types { get; set; }

//    public List<string> Colors { get; set; }

//    public List<IFormFile>? ImageFiles { get; set; }
//    public List<string>? ImageUrls { get; set; }
//}

//public class ProductSizeDto2
//{
//    public string SizeAR { get; set; }
//    public string SizeEN { get; set; }
//}

//public class ProductTypeDto2
//{
//    public string TypeAR { get; set; }
//    public string TypeEN { get; set; }
//}

//public class ProductColorDto2
//{
//    public string ColorAR { get; set; }
//    public string ColorEN { get; set; }
//}
