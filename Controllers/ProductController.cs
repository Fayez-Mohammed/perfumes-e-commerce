using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WallsShop.DTO;
using WallsShop.Repository;
namespace WallsShop.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController(ProductRepository products , IWebHostEnvironment env) : ControllerBase
{    
     /// <summary>
     /// Gets a single product by its identifier, localized according to the requested language. When the user is authenticated, user-specific data (such as wishlist state) is included.
     /// </summary>
     /// <param name="queryParameters">Query parameters including the product id, language code, and paging options.</param>
     /// <returns>An object containing the requested product under the Response property (PagedResult of ProductResponseDto for Arabic or ProductTranslationDto for English).</returns>
     [ProducesResponseType(typeof(PagedResult<ProductResponseDto>), StatusCodes.Status200OK)]
     [HttpGet("product")]
     public async Task<IActionResult> GetProductById
          ([FromQuery] QueryParameters queryParameters)
     {
        string? userId = null;
        if(User.Identity != null && User.Identity.IsAuthenticated)
        {
            userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }
          if (queryParameters.LanguageCode == "ar" || string.IsNullOrEmpty(queryParameters.LanguageCode))
          {
               var list = await products.GetArabicProductById(queryParameters,userId);

               return Ok(new { Response = list });
          }
          var list1 = await products.GetEnglishProductById(queryParameters,userId);
          
          return Ok(new {Response =list1});
     }
     
     /// <summary>
     /// Gets a paginated overview of products, localized according to the requested language. When the user is authenticated, user-specific data is included.
     /// </summary>
     /// <param name="queryParameters">Query parameters including search, order, category, paging, and language code.</param>
     /// <returns>An object containing the product list under the Response property.</returns>
     [ProducesResponseType(typeof(PagedResult<ProductOverviewDto>), StatusCodes.Status200OK)]
     [HttpGet("products")]
     public async Task<IActionResult> GetProducts
          ([FromQuery] QueryParameters queryParameters)
    {
        string? userId = null;
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }
        if (queryParameters.LanguageCode == "ar" || string.IsNullOrEmpty(queryParameters.LanguageCode))
          {
               var list = await products.GetProductsOverview(queryParameters,userId);

               return Ok(new { Response = list });
          }
          var list1 = await products.GetProductsTranslationOverview(queryParameters,userId);

          return Ok(new {Response =list1});
     }
     
     /// <summary>
     /// Gets products related to the given product by its category, localized according to the requested language.
     /// </summary>
     /// <param name="queryParameters">Query parameters including the product id, paging and language code.</param>
     /// <returns>An object containing the related product list under the Response property.</returns>
     [ProducesResponseType(typeof(PagedResult<ProductOverviewDto>), StatusCodes.Status200OK)]
     [HttpGet("related-product")]
     public async Task<IActionResult> GetRelatedProducts
          ([FromQuery] QueryParameters queryParameters)
    {
        string? userId = null;
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }
        if (queryParameters.LanguageCode == "ar" || string.IsNullOrEmpty(queryParameters.LanguageCode))
          {

               var list = await products.GetProductsByCategory(queryParameters, queryParameters.id, userId);

              // list = list.Where(p => p.Id != id).ToList();

               return Ok(new { Response = list });
          }
          var list1 = await products.GetProductTranslationsByCategory(queryParameters, queryParameters.id, userId);
          
        //  list1 = list1.Where(p => p.Id != id).ToList();
          
          return Ok(new {Response =list1});
     }
     
     /// <summary>
     /// Gets the four most recently added products, localized according to the requested language.
     /// </summary>
     /// <param name="queryParameters">Query parameters including paging and language code.</param>
     /// <returns>An object containing the recent products under the Response property.</returns>
     [ProducesResponseType(typeof(List<ProductOverviewDto>), StatusCodes.Status200OK)]
     [HttpGet("top-recent-product")]
     public async Task<IActionResult> GetTopRecentProducts
          ([FromQuery] QueryParameters queryParameters)
    {
        string? userId = null;
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }
        if (queryParameters.LanguageCode == "ar" || string.IsNullOrEmpty(queryParameters.LanguageCode))
          {

               var list = await products.GetTop4RecentProducts(queryParameters,userId);

               return Ok(new { Response = list });
          }
          var list1 = await products.GetTop4RecentProductTranslations(queryParameters,userId);

          return Ok(new {Response =list1});
     }

     /// <summary>
     /// Gets the four highest-rated products, localized according to the requested language.
     /// </summary>
     /// <param name="queryParameters">Query parameters including paging and language code.</param>
     /// <returns>An object containing the top-rated products under the Response property.</returns>
     [ProducesResponseType(typeof(List<ProductOverviewDto>), StatusCodes.Status200OK)]
     [HttpGet("top-rated-product")]
     public async Task<IActionResult> GetTopRatedProducts
          ([FromQuery] QueryParameters queryParameters)
    {
        string? userId = null;
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }
        if (queryParameters.LanguageCode == "ar" || string.IsNullOrEmpty(queryParameters.LanguageCode))
          {

               var list = await products.GetTop4RatedProducts(queryParameters,userId);

               return Ok(new { Response = list });
          }
          var list1 = await  products.GetTop4RatedProductTranslations(queryParameters,userId);
          
          return Ok(new {Response =list1});
     }

    //[HttpPut("UpdateProductById")]
    //public async Task<IActionResult> UpdateProductById([FromQuery] int id, [FromBody] UpdateProductDTO updateProductDTO)
    //{
    //    var product=await products.GetArabicProductById(new QueryParameters { PageNumber = 1, PageSize = 1 }, null);
    //}

}