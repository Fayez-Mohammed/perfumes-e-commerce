using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WallsShop.DTO;
using WallsShop.Repository;

namespace WallsShop.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class VideoController(VideoRepository repo) : ControllerBase
{
    /// <summary>Gets all active videos ordered by display order and creation date.</summary>
    /// <returns>An object containing the active videos list under the response property.</returns>
    [HttpGet("videos")]
    [ProducesResponseType(typeof(List<VideoResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVideos()
    {
        var videos = await repo.GetVideos();
        return Ok(new { response = videos });
    }

    /// <summary>Gets a single active video by its identifier.</summary>
    /// <param name="id">The identifier of the video.</param>
    /// <returns>An object containing the video under the response property, or 404 if not found or inactive.</returns>
    [HttpGet("video")]
    [ProducesResponseType(typeof(VideoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVideoById([FromQuery] int id)
    {
        var video = await repo.GetVideoById(id);
        if (video == null)
            return NotFound(new { message = "Video not found" });

        return Ok(new { response = video });
    }
}
