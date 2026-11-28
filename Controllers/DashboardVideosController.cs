using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WallsShop.DTO;
using WallsShop.DTO.Dashboard;
using WallsShop.Repository.Dashboard;

namespace WallsShop.Controllers.Dashboard;

[ApiController]
[Route("api/dashboard/[controller]")]
[Authorize(Roles = "Admin")]
public class DashboardVideosController : ControllerBase
{
    private const long MaxVideoRequestSize = 104_857_600; // 100 MB

    private readonly DashboardVideoRepository _repository;
    private readonly ILogger<DashboardVideosController> _logger;

    public DashboardVideosController(
        DashboardVideoRepository repository,
        ILogger<DashboardVideosController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>Retrieves all dashboard videos (active and inactive), ordered by display order and creation date.</summary>
    /// <returns>A JSON object containing the list of videos; 500 on failure.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<DashboardVideoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllVideos()
    {
        try
        {
            var videos = await _repository.GetAllVideos();
            return Ok(new { success = true, data = videos });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all videos");
            return StatusCode(500, new { success = false, message = "حدث خطأ أثناء جلب الفيديوهات" });
        }
    }

    /// <summary>Retrieves a single video by its identifier.</summary>
    /// <param name="id">The identifier of the video.</param>
    /// <returns>A JSON object containing the video; 404 if not found; 500 on failure.</returns>
    [HttpGet("GetVideoById")]
    [ProducesResponseType(typeof(DashboardVideoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetVideoById([FromQuery] int id)
    {
        try
        {
            var video = await _repository.GetVideoById(id);
            if (video == null)
                return NotFound(new { success = false, message = "الفيديو غير موجود" });

            return Ok(new { success = true, data = video });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching video {VideoId}", id);
            return StatusCode(500, new { success = false, message = "حدث خطأ أثناء جلب الفيديو" });
        }
    }

    /// <summary>Uploads or links a new video along with its cover image.</summary>
    /// <param name="dto">The video data, including uploaded video/cover files or external URLs.</param>
    /// <returns>The created video as a JSON object; 400 for invalid data or creation failure; 500 on failure.</returns>
    [HttpPost("CreateVideo")]
    [RequestSizeLimit(MaxVideoRequestSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxVideoRequestSize)]
    [ProducesResponseType(typeof(DashboardVideoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateVideo([FromForm] CreateUpdateVideoDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "بيانات غير صحيحة", errors = ModelState });

            var (success, message, videoId) = await _repository.CreateVideo(dto);
            if (!success)
                return BadRequest(new { success = false, message });

            return await GetVideoById(videoId ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating video");
            return StatusCode(500, new { success = false, message = "حدث خطأ أثناء إنشاء الفيديو" });
        }
    }

    /// <summary>Updates an existing video, optionally replacing its video file, cover image, status, or display order.</summary>
    /// <param name="id">The identifier of the video to update.</param>
    /// <param name="dto">The updated video data.</param>
    /// <returns>The updated video as a JSON object; 400 for invalid data or update failure; 404 if not found; 500 on failure.</returns>
    [HttpPut("UpdateVideo")]
    [RequestSizeLimit(MaxVideoRequestSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxVideoRequestSize)]
    [ProducesResponseType(typeof(DashboardVideoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateVideo([FromQuery] int id, [FromForm] CreateUpdateVideoDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "بيانات غير صحيحة", errors = ModelState });

            var (success, message, videoId) = await _repository.UpdateVideo(id, dto);
            if (!success)
                return BadRequest(new { success = false, message });

            return await GetVideoById(videoId ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating video {VideoId}", id);
            return StatusCode(500, new { success = false, message = "حدث خطأ أثناء تحديث الفيديو" });
        }
    }

    /// <summary>Deletes a video and removes any locally hosted video and cover image files.</summary>
    /// <param name="id">The identifier of the video to delete.</param>
    /// <returns>A JSON object with a success or failure message; 400 if deletion fails; 500 on failure.</returns>
    [HttpDelete("DeleteVideo")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteVideo([FromQuery] int id)
    {
        try
        {
            var (success, message) = await _repository.DeleteVideo(id);
            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting video {VideoId}", id);
            return StatusCode(500, new { success = false, message = "حدث خطأ أثناء حذف الفيديو" });
        }
    }
}
