using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WallsShop.DTO;
using WallsShop.DTO.Dashboard;
using WallsShop.Repository.Dashboard;

namespace WallsShop.Controllers.Dashboard;


[ApiController]
[Route("api/dashboard/[controller]")]
[Authorize(Roles = "Admin")] 
[Authorize] 
public class DashboardOffersController : ControllerBase
{
    private readonly DashboardOfferRepository _repository;
    private readonly ILogger<DashboardOffersController> _logger;

    public DashboardOffersController(
        DashboardOfferRepository repository,
        ILogger<DashboardOffersController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

   
    /// <summary>Retrieves all dashboard offers.</summary>
    /// <returns>A JSON object containing the list of offers; 500 on failure.</returns>
    [HttpGet("GetOffers")]
    [ProducesResponseType(typeof(List<DashboardOfferDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllOffers()
    {
        try
        {
            var offers = await _repository.GetAllOffers();
            return Ok(new { success = true, data = offers });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all offers");
            return StatusCode(500, new { success = false, message = "حدث خطأ أثناء جلب العروض" });
        }
    }

  
    /// <summary>Retrieves a single offer by its identifier.</summary>
    /// <param name="id">The identifier of the offer.</param>
    /// <returns>A JSON object containing the offer; 404 if not found; 500 on failure.</returns>
    [HttpGet("SpecificOffer")]
    [ProducesResponseType(typeof(DashboardOfferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOfferById([FromQuery] int id)
    {
        try
        {
            var offer = await _repository.GetOfferById(id);
            
            if (offer == null)
                return NotFound(new { success = false, message = "العرض غير موجود" });

            return Ok(new { success = true, data = offer });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching offer {OfferId}", id);
            return StatusCode(500, new { success = false, message = "حدث خطأ أثناء جلب العرض" });
        }
    }

  
    /// <summary>Creates a new offer.</summary>
    /// <param name="dto">The offer data, including any uploaded image.</param>
    /// <returns>The created offer as a JSON object; 400 for invalid data or creation failure; 404 if the offer cannot be retrieved afterwards; 500 on failure.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(DashboardOfferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateOffer([FromForm] CreateUpdateOfferDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "بيانات غير صحيحة", errors = ModelState });

            var (success, message, offerId) = await _repository.CreateOffer(dto);

            if (!success)
                return BadRequest(new { success = false, message });

            return await GetOfferById(offerId ?? 0);

            //return CreatedAtAction(
            //    nameof(GetOfferById),
            //    new { id = offerId },
            //    new { success = true, message, offerId }
           // );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating offer");
            return StatusCode(500, new { success = false, message = "حدث خطأ أثناء إنشاء العرض" });
        }
    }


    /// <summary>Updates an existing offer.</summary>
    /// <param name="id">The identifier of the offer to update.</param>
    /// <param name="dto">The updated offer data, including any uploaded image.</param>
    /// <returns>The updated offer as a JSON object; 400 for invalid data or update failure; 404 if the offer cannot be retrieved afterwards; 500 on failure.</returns>
    [HttpPut("UpdateOffer")]
    [ProducesResponseType(typeof(DashboardOfferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateOffer([FromQuery] int id, [FromForm] CreateUpdateOfferDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "بيانات غير صحيحة", errors = ModelState });

            var (success, message, offerId) = await _repository.UpdateOffer(id, dto);

            if (!success)
                return BadRequest(new { success = false, message });

            return await GetOfferById(offerId ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating offer {OfferId}", id);
            return StatusCode(500, new { success = false, message = "حدث خطأ أثناء تحديث العرض" });
        }
    }

   
    /// <summary>Deletes an offer.</summary>
    /// <param name="id">The identifier of the offer to delete.</param>
    /// <returns>A JSON object with a success or failure message; 400 if deletion fails; 500 on failure.</returns>
    [HttpDelete("DeleteOffer")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteOffer([FromQuery] int id)
    {
        try
        {
            var (success, message) = await _repository.DeleteOffer(id);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting offer {OfferId}", id);
            return StatusCode(500, new { success = false, message = "حدث خطأ أثناء حذف العرض" });
        }
    }
}
