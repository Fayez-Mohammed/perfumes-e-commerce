using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WallsShop.DTO;
using WallsShop.DTO.Dashboard;
using WallsShop.Repository.Dashboard;

namespace WallsShop.Controllers.Dashboard;


[ApiController]
[Route("api/dashboard/[controller]")]
[Authorize(Roles = "Admin")]
public class DashboardCategoriesController : ControllerBase
{
    private readonly DashboardCategoryRepository _repository;
    private readonly ILogger<DashboardCategoriesController> _logger;

    public DashboardCategoriesController(
        DashboardCategoryRepository repository,
        ILogger<DashboardCategoriesController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>Retrieves all dashboard categories.</summary>
    /// <returns>A JSON object containing the list of categories; 500 on failure.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<DashboardCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllCategories()
    {
        try
        {
            var categories = await _repository.GetAllCategories();
            return Ok(new { success = true, data = categories });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all categories");
            return StatusCode(500, new { success = false, message = "حدث خطأ أثناء جلب الأقسام" });
        }
    }   
       
       
    /// <summary>Retrieves a single category by its identifier.</summary>
    /// <param name="id">The identifier of the category.</param>
    /// <returns>A JSON object containing the category; 404 if not found; 500 on failure.</returns>
    [HttpGet("GetCategoryById")]
    [ProducesResponseType(typeof(DashboardCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCategoryById([FromQuery] int id)
    {
        try
        {
            var category = await _repository.GetCategoryById(id);

            if (category == null)
                return NotFound(new { success = false, message = "القسم غير موجود" });

            return Ok(new { success = true, data = category });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching category {CategoryId}", id);
            return StatusCode(500, new { success = false, message = "حدث خطأ أثناء جلب القسم" });
        }
    }


    /// <summary>Creates a new category.</summary>
    /// <param name="dto">The category data, including any uploaded image.</param>
    /// <returns>The created category as a JSON object; 400 for invalid data or creation failure; 404 if the category cannot be retrieved afterwards; 500 on failure.</returns>
    [HttpPost("CreateCategory")]
    [ProducesResponseType(typeof(DashboardCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCategory([FromForm] CreateUpdateCategoryDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "بيانات غير صحيحة", errors = ModelState });

            var (success, message, categoryId) = await _repository.CreateCategory(dto);

            if (!success)
                return BadRequest(new { success = false, message });

            return await GetCategoryById(categoryId ?? 0);
            //return CreatedAtAction(
            //    nameof(GetCategoryById),
            //    new { id = categoryId },
            //    new { success = true, message, categoryId }
            //);
        }               
                          
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return StatusCode(500, new { success = false, message = "حدث خطأ أثناء إنشاء القسم" });
        }
    }


    /// <summary>Updates an existing category.</summary>
    /// <param name="id">The identifier of the category to update.</param>
    /// <param name="dto">The updated category data, including any uploaded image.</param>
    /// <returns>The updated category as a JSON object; 400 for invalid data or update failure; 404 if the category cannot be retrieved afterwards; 500 on failure.</returns>
    [HttpPut("UpdateCategory")]
    [ProducesResponseType(typeof(DashboardCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateCategory([FromQuery] int id, [FromForm] CreateUpdateCategoryDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "بيانات غير صحيحة", errors = ModelState });

            var (success, message, categoryId) = await _repository.UpdateCategory(id, dto);

            if (!success)
                return BadRequest(new { success = false, message });

            return await GetCategoryById(categoryId ?? 0);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {CategoryId}", id);
            return StatusCode(500, new { success = false, message = "حدث خطأ أثناء تحديث القسم" });
        }
    }


    /// <summary>Deletes a category.</summary>
    /// <param name="id">The identifier of the category to delete.</param>
    /// <returns>A JSON object with a success or failure message; 400 if deletion fails; 500 on failure.</returns>
    [HttpDelete("DeleteCategory")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        try
        {
            var (success, message) = await _repository.DeleteCategory(id);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", id);
            return StatusCode(500, new { success = false, message = "حدث خطأ أثناء حذف القسم" });
        }
    }
}
