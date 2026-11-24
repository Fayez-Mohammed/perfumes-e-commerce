using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;
using System.Text.Json;
using WallsShop.DTO;
using WallsShop.Services;

namespace WallsShop.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly CartService _cartService;

    public CartController(CartService cartService) => _cartService = cartService;
    
    /// <summary>Adds an item to the authenticated user's cart.</summary>
    /// <param name="itemDto">The cart item (product, variant, color and quantity) to add.</param>
    /// <returns>A success message confirming the item was added.</returns>
    [Authorize]
    [HttpPost("add-item")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async   Task<IActionResult> AddToCart([FromBody] CartItem itemDto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await _cartService.AddToCart(userId, itemDto);
       
        return Ok(new { Message = "Item added to cart successfully" });
    }

    //[HttpPost("get-cart")]
    //public async Task<IActionResult> GetCartItems([FromBody] GetCartDto getCartDto, [FromQuery] string languageCode = "ar")
    //{
    //    var authenticatedUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    //    List<CartItem> finalItems = new List<CartItem>();
    //    if (string.IsNullOrEmpty(authenticatedUserId))
    //    {
    //        if (getCartDto?.ShoppingCart?.Items == null || !getCartDto.ShoppingCart.Items.Any())
    //        {
    //            return Ok(new { cart = new List<CartItem>() });
    //        }
    //        return Ok(new {cart =  await _cartService.GetProductImageName(getCartDto.ShoppingCart.Items, languageCode)});
    //    }
    //    else
    //    {
    //        finalItems = await _cartService.GetCartItems(authenticatedUserId, languageCode);
    //        //var items = new ShoppingCart()
    //        //{
    //        //    Items = await _cartService.GetCartItems(authenticatedUserId, languageCode),
    //        //    UserId = getCartDto.UserId
    //        //};
    //        //return Ok(new {cart =items});
    //    }
    //    var summary = new OrderSummaryDto
    //    {
    //        TotalOriginalPrice = finalItems.Sum(i => i.OriginalPrice * i.Quantity),

    //        TotalPrice = finalItems.Sum(i => i.UnitPrice * i.Quantity),

    //        TotalProductsCount = finalItems.Sum(i => i.Quantity)
    //    };


    //    summary.TotalDiscount = summary.TotalPrice - summary.TotalOriginalPrice;

    //    return Ok(new CartResponseDto
    //    {
    //        Items = finalItems,
    //        Summary = summary
    //    });
    //}

    /// <summary>Retrieves the cart items with their price summary, for either the authenticated user or an anonymous cart.</summary>
    /// <param name="getCartDto">The anonymous shopping cart payload used when the user is not authenticated.</param>
    /// <param name="languageCode">The language code used for localized product data; defaults to "ar".</param>
    /// <returns>The cart items and their order summary.</returns>
    [HttpPost("get-cart")]
    [ProducesResponseType(typeof(CartResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCartItems([FromBody] GetCartDto getCartDto, [FromQuery] string languageCode = "ar")
    {
        var authenticatedUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        List<CartItem> finalItems = new List<CartItem>();

        if (string.IsNullOrEmpty(authenticatedUserId))
        {
          
            if (getCartDto?.ShoppingCart?.Items != null && getCartDto.ShoppingCart.Items.Any())
            {
                finalItems = await _cartService.GetProductImageName(getCartDto.ShoppingCart.Items, languageCode);
               
            }
        }
        else
        {
            finalItems = await _cartService.GetCartItems(authenticatedUserId, languageCode);
        }

      
        var summary = new OrderSummaryDto
        {
            TotalOriginalPrice = finalItems.Sum(i => i.OriginalPrice * i.Quantity),
            TotalPrice = finalItems.Sum(i => i.UnitPrice * i.Quantity),
            TotalProductsCount = finalItems.Sum(i => i.Quantity),
            Count = finalItems.Count
        };

        summary.TotalDiscount = summary.TotalPrice - summary.TotalOriginalPrice;
       
        return Ok(new CartResponseDto
        {
            Items = finalItems,
            Summary = summary
        });
    }
    /// <summary>Gets the number of items in the cart for the authenticated user or an anonymous cart.</summary>
    /// <param name="getCartDto">The anonymous shopping cart payload used when the user is not authenticated.</param>
    /// <param name="languageCode">The language code used for localized product data; defaults to "ar".</param>
    /// <returns>The total count of items in the cart.</returns>
    [HttpPost("get-Count-Of-cart")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCartItemsCount([FromBody] GetCartDto getCartDto, [FromQuery] string languageCode = "ar")
    {
        var authenticatedUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        int count = 0;

        if (string.IsNullOrEmpty(authenticatedUserId))
        {
          
            if (getCartDto?.ShoppingCart?.Items != null && getCartDto.ShoppingCart.Items.Any())
            {
                count = await _cartService.GetProductImageNameCount(getCartDto.ShoppingCart.Items, languageCode);
               
            }
        }
        else
        {
            count = await _cartService.GetCartItemsCount(authenticatedUserId, languageCode);
        }

      
       

       
        return Ok(count);
    }

    // داخل CartController.cs

    /// <summary>Updates the quantity of a specific item in the authenticated user's cart.</summary>
    /// <param name="request">Identifies the cart line (product, variant, color) and its new quantity.</param>
    /// <param name="languageCode">The language code used for localized product data; defaults to "ar".</param>
    /// <returns>The updated cart items and order summary, or the request result.</returns>
    [HttpPost("update-quantity")]
    [ProducesResponseType(typeof(CartResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequestDto request, [FromQuery] string languageCode = "ar")
    {
        
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

     
        var updatedItems = await _cartService.UpdateQuantityAsync(
            userId,
            request.ProductId,
            request.VarianceId,
            request.Quantity,
            request.ColorId,
            languageCode
        );

        if (updatedItems == null)
        {
            return NotFound(new { Message = "Product not found in cart" });
        }

        var summary = new OrderSummaryDto
        {
            TotalOriginalPrice = updatedItems.Sum(i => i.OriginalPrice * i.Quantity),
            TotalPrice = updatedItems.Sum(i => i.UnitPrice * i.Quantity),
            TotalProductsCount = updatedItems.Sum(i => i.Quantity),
            Count = updatedItems.Count
        };

        summary.TotalDiscount = summary.TotalPrice - summary.TotalOriginalPrice;

        return Ok(new CartResponseDto
        {
            Items = updatedItems,
            Summary = summary
        });
    }
    // داخل CartController.cs
    /// <summary>Recomputes and returns the cart items and order summary for the authenticated user or an anonymous cart.</summary>
    /// <param name="getCartDto">The anonymous shopping cart payload used when the user is not authenticated.</param>
    /// <param name="languageCode">The language code used for localized product data; defaults to "ar".</param>
    /// <returns>The refreshed cart items and their order summary.</returns>
    [HttpPost("Update-cart-items")]
    [ProducesResponseType(typeof(CartResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCartItems([FromBody] GetCartDto getCartDto, [FromQuery] string languageCode = "ar")
    {
        var authenticatedUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        List<CartItem> finalItems = new List<CartItem>();

        if (string.IsNullOrEmpty(authenticatedUserId))
        {

            if (getCartDto?.ShoppingCart?.Items != null && getCartDto.ShoppingCart.Items.Any())
            {
                finalItems = await _cartService.GetProductImageName(getCartDto.ShoppingCart.Items, languageCode);
            }
        }
        else
        {
            finalItems = await _cartService.GetCartItems(authenticatedUserId, languageCode);
        }


        var summary = new OrderSummaryDto
        {
            TotalOriginalPrice = finalItems.Sum(i => i.OriginalPrice * i.Quantity),
            TotalPrice = finalItems.Sum(i => i.UnitPrice * i.Quantity),
            TotalProductsCount = finalItems.Sum(i => i.Quantity),
            Count = finalItems.Count

        };

        summary.TotalDiscount = summary.TotalPrice - summary.TotalOriginalPrice;

        return Ok(new CartResponseDto
        {
            Items = finalItems,
            Summary = summary
        });
    }
    /// <summary>Removes a specific item from the authenticated user's cart.</summary>
    /// <param name="request">Identifies the cart line (product, variant, color) to remove.</param>
    /// <param name="languageCode">The language code used for localized product data; defaults to "ar".</param>
    /// <returns>The remaining cart items and order summary, or the request result.</returns>
    [HttpDelete("delete-item")]
    [ProducesResponseType(typeof(CartResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem([FromBody] UpdateQuantityRequestDto request, [FromQuery] string languageCode = "ar")
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

      
        var updatedItems = await _cartService.RemoveItemAsync(
            userId,
            request.ProductId,
            request.VarianceId,
            request.ColorId,
            languageCode
        );

        if (updatedItems == null)
        {
            return NotFound(new { Message = "Item not found in cart" });
        }

        var summary = new OrderSummaryDto
        {
            TotalOriginalPrice = updatedItems.Sum(i => i.OriginalPrice * i.Quantity),
            TotalPrice = updatedItems.Sum(i => i.UnitPrice * i.Quantity),
            TotalProductsCount = updatedItems.Sum(i => i.Quantity),
            Count = updatedItems.Count
        };

        summary.TotalDiscount = summary.TotalPrice - summary.TotalOriginalPrice;

        return Ok(new CartResponseDto
        {
            Items = updatedItems,
            Summary = summary
        });
    }
    /// <summary>Gets the number of items in the cart of the current (possibly anonymous) user.</summary>
    /// <returns>An object containing the item count.</returns>
    [HttpGet("CountOfItemsInCartForUser")]
    [ProducesResponseType(typeof(CountResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CountOfItemsInCartForUser()
    {
        var authenticatedUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        int count = 0;

       
        count = await _cartService.GetCartItemsCount(authenticatedUserId, "ar");
        


        return Ok(new { Count = count });
    }
    //[HttpGet("CountOfItemsInCartForUser")]
    //public async Task<IActionResult> CountOfItemsInCartForUser()
    //{
    //    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    //    if (string.IsNullOrEmpty(userId)) return Unauthorized("you are not authorized");

    //    var count =  _cartService.GetCountOfCartsForSpecificUser(userId);

    //    return Ok(new { Count = count });
    //}
}