using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WallsShop.DTO;
using WallsShop.Repository;

namespace WallsShop.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WishlistController(WishlistRepository repo , ProductRepository product ) : ControllerBase 
{
    /// <summary>Adds a product to the authenticated user's wishlist.</summary>
    /// <param name="wishlistDto">The wishlist item to add.</param>
    /// <returns>A success message confirming the item was added.</returns>
    [HttpPost("add")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async  Task<IActionResult> Add([FromBody] WhishlistDto wishlistDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await repo.AddToWishlist(wishlistDto, userId!);
        return Ok(new { Message = "Item added to wishlist successfully" });
    }
    /// <summary>Gets the wishlist of the authenticated user, or the details of the supplied wishlist items for anonymous users.</summary>
    /// <param name="wishlistDto">The wishlist items to resolve when the user is not authenticated.</param>
    /// <param name="LanguageCode">The language code used for localized product data; defaults to "en".</param>
    /// <returns>The user's wishlist when authenticated, otherwise the resolved products.</returns>
    [AllowAnonymous]
    [HttpGet("get")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] WhishlistDto wishlistDto, [FromQuery] string LanguageCode="en")
    {

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (User.Identity.IsAuthenticated)
        {
            var response = await repo.GetWishlist(userId!,LanguageCode);
            return Ok(new { response });
        }
        var products = await repo.GetProducts(wishlistDto,LanguageCode);
        return Ok(new { products });
    }
    /// <summary>Gets the wishlist item count for an anonymous user from the supplied product identifiers.</summary>
    /// <param name="ProductIds">The product identifiers to count in the wishlist.</param>
    /// <returns>The wishlist count for the supplied products.</returns>
    [AllowAnonymous]
    [HttpGet("CountWishlistItemsForNotAuthenticated")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> WithListCount([FromQuery] List<int> ProductIds)
    {
        
        var products = await repo.GetWishlistCount(ProductIds);
        return Ok(new { products });
    }

    /// <summary>Gets the number of wishlist items belonging to the authenticated user.</summary>
    /// <returns>An object containing the wishlist count, or the request result when the user is not authenticated.</returns>
    [HttpGet("CountOfWishlistsByUserId")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public IActionResult CountOfWithlists()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if(userId == null)
        {
            return BadRequest(new { Message = "User not authenticated" });
        }
        var count = repo.CountOFWishlistForSpecificUser(userId);
        return Ok(new { Count = count });
    }


    /// <summary>Removes a product from the authenticated user's wishlist.</summary>
    /// <param name="productId">The identifier of the product to remove.</param>
    /// <returns>A success message confirming the item was removed.</returns>
    [HttpDelete("remove")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Remove([FromQuery] int productId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await repo.RemoveFromWishlist(userId!, productId);
        return Ok(new { Message = "Item removed from wishlist successfully" });
    }

}