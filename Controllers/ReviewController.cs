using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WallsShop.DTO;
using WallsShop.Entity;
using WallsShop.Repository;

namespace WallsShop.Controllers;

[ApiController]
[Route("api/[controller]")]

public class ReviewController(ReviewRepository repo,UserManager<User> _userManager ) : ControllerBase 
{
    // [AllowAnonymous]
    //[HttpGet("reviews")]
    //public async Task<IActionResult> GetReviews([FromQuery] int productId)
    //{
    //    var userName = await _userManager.GetUserAsync(User);
    //    bool isAdmin = User.IsInRole("Admin");
    //    var reviews = await repo.GetProductReviews(productId, userName?.Name ?? "", isAdmin);

    //    return Ok(new { response = reviews });


    //}

   // [Authorize]
    
    /// <summary>
    /// Gets the reviews for a product, including the average rating, total review count, and per-review deletion permissions for the current user.
    /// </summary>
    /// <param name="productId">The identifier of the product whose reviews are requested.</param>
    /// <returns>An object containing the product's review details under the response property.</returns>
    [ProducesResponseType(typeof(ReviewResponseDto), StatusCodes.Status200OK)]
    [HttpGet("reviews")]
    public async Task<IActionResult> GetReviews([FromQuery] int productId)
    {
        var user = await _userManager.GetUserAsync(User);
        bool isAdmin = User.IsInRole("Admin");
        string userId = user?.Id ?? ""; // Changed from Name to UserName

        var reviews = await repo.GetProductReviews(productId, userId, isAdmin);
        return Ok(new { response = reviews });
    }

    /// <summary>
    /// Creates a review for a product by the currently authenticated user and updates the product's rating aggregates.
    /// </summary>
    /// <param name="review">The review data, including the product id, rating, and comment.</param>
    /// <param name="LanguageCode">The language code used for localized error messages (defaults to "en").</param>
    /// <returns>The updated review details for the product.</returns>
    [ProducesResponseType(typeof(ReviewResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [Authorize]
    [HttpPost("create-review")]
    public async Task<IActionResult> CreateReview([FromBody] ReviewDto review, [FromQuery] string LanguageCode = "en")
    {
        if(User.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new {message = LanguageCode == "ar" ? "غير مصرح" : "Unauthorized"});
        var result = await repo.CreateReview( review , User , _userManager);
        if (result==null)
            return BadRequest(new {message = LanguageCode == "ar" ? "خطأ أثناء إنشاء المراجعة" : "Error while creating review"});

        return Ok(result);
    }
    /// <summary>
    /// Deletes a review by its identifier and recalculates the product's rating aggregates.
    /// </summary>
    /// <param name="id">The identifier of the review to delete.</param>
    /// <param name="LanguageCode">The language code used for localized error messages (defaults to "en").</param>
    /// <returns>The updated review details for the affected product.</returns>
    [ProducesResponseType(typeof(ReviewResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [Authorize]
    [HttpDelete("delete-review")]
    public async Task<IActionResult> DeleteReview([FromQuery] int id, [FromQuery] string LanguageCode = "en")
    {
        var (status, result) = await repo.DeleteReview(id, User, _userManager);
        return status switch
        {
            DeleteReviewStatus.NotFound => NotFound(new { message = LanguageCode == "ar" ? "المراجعة غير موجودة" : "Review not found" }),
            DeleteReviewStatus.Forbidden => Forbid(),
            DeleteReviewStatus.Error => BadRequest(new { message = LanguageCode == "ar" ? "حدث خطأ ما" : "Some thing wrong happend" }),
            _ => Ok(result)
        };
    }
    /// <summary>
    /// Gets all reviews with user and product details for administrative management.
    /// </summary>
    /// <param name="LanguageCode">The language code used to select the product name (defaults to "ar").</param>
    /// <returns>An object containing all review details under the response property.</returns>
    [ProducesResponseType(typeof(List<ReviewResponseDto2>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [Authorize(Roles ="Admin")]
    [HttpGet("all-reviews-admin")]
    public async Task<IActionResult> GetAllReviewsAdmin([FromQuery] string LanguageCode = "ar")
    {
       

        var reviews = await repo.GetAllReviewsWithDetailsAsync(LanguageCode);

        return Ok(new { response = reviews });
    }

    /// <summary>
    /// Gets the top 10 highest-rated (and most recent) reviews across all products. Accessible to both authenticated and anonymous users.
    /// </summary>
    /// <param name="LanguageCode">The language code used to localize the product name ("en" or "ar", defaults to "en").</param>
    /// <returns>An object containing the top 10 reviews under the response property.</returns>
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<PublicReviewDto>), StatusCodes.Status200OK)]
    [HttpGet("top-reviews")]
    public async Task<IActionResult> GetTop10Reviews([FromQuery] string LanguageCode = "en")
    {
        var reviews = await repo.GetTop10ReviewsAsync(LanguageCode);
        return Ok(new { response = reviews });
    }
}