using Microsoft.AspNetCore.Mvc;
using WallsShop.DTO;
using WallsShop.Repository;

namespace WallsShop.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoryController(CategoryRepository repo) : ControllerBase
{
    /// <summary>Gets the list of categories in the requested language.</summary>
    /// <param name="LanguageCode">The language code used to localize the categories.</param>
    /// <returns>An object containing the localized categories under the response property.</returns>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<CategoryResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories([FromQuery] string LanguageCode )
    {
        return Ok(new {response = await repo.GetCategories(LanguageCode)});
    }
}