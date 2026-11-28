using Microsoft.EntityFrameworkCore;
using WallsShop.Context;
using WallsShop.DTO;

namespace WallsShop.Repository;

public class VideoRepository(WallShopContext ctx)
{
    public async Task<List<VideoResponseDto>> GetVideos()
    {
        return await ctx.Videos
            .AsNoTracking()
            .Where(v => v.IsActive)
            .OrderBy(v => v.DisplayOrder)
            .ThenByDescending(v => v.CreatedAt)
            .Select(v => new VideoResponseDto
            {
                Id = v.Id,
                VideoUrl = v.VideoUrl,
                CoverImageUrl = v.CoverImageUrl,
                DisplayOrder = v.DisplayOrder,
                CreatedAt = v.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<VideoResponseDto?> GetVideoById(int id)
    {
        return await ctx.Videos
            .AsNoTracking()
            .Where(v => v.Id == id && v.IsActive)
            .Select(v => new VideoResponseDto
            {
                Id = v.Id,
                VideoUrl = v.VideoUrl,
                CoverImageUrl = v.CoverImageUrl,
                DisplayOrder = v.DisplayOrder,
                CreatedAt = v.CreatedAt
            })
            .FirstOrDefaultAsync();
    }
}
