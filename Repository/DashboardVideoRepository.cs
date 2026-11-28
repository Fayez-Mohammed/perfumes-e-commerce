using Microsoft.EntityFrameworkCore;
using WallsShop.Context;
using WallsShop.DTO.Dashboard;
using WallsShop.Entity;

namespace WallsShop.Repository.Dashboard;

public class DashboardVideoRepository
{
    private static readonly string[] AllowedVideoExtensions = [".mp4", ".webm", ".mov"];
    private static readonly string[] AllowedCoverExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    private readonly WallShopContext _ctx;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public DashboardVideoRepository(
        WallShopContext ctx,
        IWebHostEnvironment env,
        IConfiguration config)
    {
        _ctx = ctx;
        _env = env;
        _config = config;
    }

    public async Task<List<DashboardVideoDto>> GetAllVideos()
    {
        return await _ctx.Videos
            .AsNoTracking()
            .OrderBy(v => v.DisplayOrder)
            .ThenByDescending(v => v.CreatedAt)
            .Select(v => new DashboardVideoDto
            {
                Id = v.Id,
                VideoUrl = v.VideoUrl,
                CoverImageUrl = v.CoverImageUrl,
                IsActive = v.IsActive,
                DisplayOrder = v.DisplayOrder,
                CreatedAt = v.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<DashboardVideoDto?> GetVideoById(int id)
    {
        return await _ctx.Videos
            .AsNoTracking()
            .Where(v => v.Id == id)
            .Select(v => new DashboardVideoDto
            {
                Id = v.Id,
                VideoUrl = v.VideoUrl,
                CoverImageUrl = v.CoverImageUrl,
                IsActive = v.IsActive,
                DisplayOrder = v.DisplayOrder,
                CreatedAt = v.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string? Message, int? VideoId)> CreateVideo(CreateUpdateVideoDto dto)
    {
        var hasVideoFile = dto.VideoFile is { Length: > 0 };
        var hasVideoLink = !string.IsNullOrWhiteSpace(dto.VideoUrlLink);
        if (!hasVideoFile && !hasVideoLink)
            return (false, "يجب توفير ملف فيديو أو رابط فيديو", null);

        var hasCoverFile = dto.CoverImageFile is { Length: > 0 };
        var hasCoverLink = !string.IsNullOrWhiteSpace(dto.CoverImageUrlLink);
        if (!hasCoverFile && !hasCoverLink)
            return (false, "يجب توفير صورة غلاف أو رابط صورة غلاف", null);

        if (hasVideoFile && !IsAllowedExtension(dto.VideoFile!, AllowedVideoExtensions))
            return (false, "صيغة الفيديو غير مدعومة. الصيغ المسموحة: .mp4, .webm, .mov", null);

        if (hasCoverFile && !IsAllowedExtension(dto.CoverImageFile!, AllowedCoverExtensions))
            return (false, "صيغة صورة الغلاف غير مدعومة. الصيغ المسموحة: .jpg, .jpeg, .png, .webp", null);

        string? savedVideoUrl = null;
        string? savedCoverUrl = null;

        try
        {
            savedVideoUrl = hasVideoFile
                ? await SaveMediaFileAsync(dto.VideoFile!, "videos")
                : dto.VideoUrlLink!.Trim();

            savedCoverUrl = hasCoverFile
                ? await SaveMediaFileAsync(dto.CoverImageFile!, Path.Combine("images", "videos"))
                : dto.CoverImageUrlLink!.Trim();

            var video = new Video
            {
                VideoUrl = savedVideoUrl,
                CoverImageUrl = savedCoverUrl,
                IsActive = dto.IsActive ?? true,
                DisplayOrder = dto.DisplayOrder ?? 0,
                CreatedAt = DateTime.UtcNow
            };

            _ctx.Videos.Add(video);
            await _ctx.SaveChangesAsync();

            return (true, "تم إنشاء الفيديو بنجاح", video.Id);
        }
        catch (Exception ex)
        {
            if (hasVideoFile && savedVideoUrl != null) DeleteLocalFile(savedVideoUrl);
            if (hasCoverFile && savedCoverUrl != null) DeleteLocalFile(savedCoverUrl);
            return (false, $"خطأ في إنشاء الفيديو: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string? Message, int? VideoId)> UpdateVideo(int id, CreateUpdateVideoDto dto)
    {
        string? newSavedVideoUrl = null;
        string? newSavedCoverUrl = null;

        var hasVideoFile = dto.VideoFile is { Length: > 0 };
        var hasVideoLink = !string.IsNullOrWhiteSpace(dto.VideoUrlLink);
        var hasCoverFile = dto.CoverImageFile is { Length: > 0 };
        var hasCoverLink = !string.IsNullOrWhiteSpace(dto.CoverImageUrlLink);

        if (hasVideoFile && !IsAllowedExtension(dto.VideoFile!, AllowedVideoExtensions))
            return (false, "صيغة الفيديو غير مدعومة. الصيغ المسموحة: .mp4, .webm, .mov", 0);

        if (hasCoverFile && !IsAllowedExtension(dto.CoverImageFile!, AllowedCoverExtensions))
            return (false, "صيغة صورة الغلاف غير مدعومة. الصيغ المسموحة: .jpg, .jpeg, .png, .webp", 0);

        try
        {
            var video = await _ctx.Videos.FindAsync(id);
            if (video == null)
                return (false, "الفيديو غير موجود", 0);

            var oldVideoUrl = video.VideoUrl;
            var oldCoverUrl = video.CoverImageUrl;
            var replaceVideo = hasVideoFile || hasVideoLink;
            var replaceCover = hasCoverFile || hasCoverLink;

            if (dto.IsActive.HasValue)
                video.IsActive = dto.IsActive.Value;

            if (dto.DisplayOrder.HasValue)
                video.DisplayOrder = dto.DisplayOrder.Value;

            if (hasVideoFile)
            {
                newSavedVideoUrl = await SaveMediaFileAsync(dto.VideoFile!, "videos");
                video.VideoUrl = newSavedVideoUrl;
            }
            else if (hasVideoLink)
            {
                video.VideoUrl = dto.VideoUrlLink!.Trim();
            }

            if (hasCoverFile)
            {
                newSavedCoverUrl = await SaveMediaFileAsync(dto.CoverImageFile!, Path.Combine("images", "videos"));
                video.CoverImageUrl = newSavedCoverUrl;
            }
            else if (hasCoverLink)
            {
                video.CoverImageUrl = dto.CoverImageUrlLink!.Trim();
            }

            _ctx.Videos.Update(video);
            await _ctx.SaveChangesAsync();

            if (replaceVideo) DeleteLocalFile(oldVideoUrl);
            if (replaceCover) DeleteLocalFile(oldCoverUrl);

            return (true, "تم تحديث الفيديو بنجاح", video.Id);
        }
        catch (Exception ex)
        {
            if (newSavedVideoUrl != null) DeleteLocalFile(newSavedVideoUrl);
            if (newSavedCoverUrl != null) DeleteLocalFile(newSavedCoverUrl);
            return (false, $"خطأ في تحديث الفيديو: {ex.Message}", 0);
        }
    }

    public async Task<(bool Success, string? Message)> DeleteVideo(int id)
    {
        try
        {
            var video = await _ctx.Videos.FindAsync(id);
            if (video == null)
                return (false, "الفيديو غير موجود");

            var videoUrl = video.VideoUrl;
            var coverUrl = video.CoverImageUrl;

            _ctx.Videos.Remove(video);
            await _ctx.SaveChangesAsync();

            DeleteLocalFile(videoUrl);
            DeleteLocalFile(coverUrl);

            return (true, "تم حذف الفيديو بنجاح");
        }
        catch (Exception ex)
        {
            return (false, $"خطأ في حذف الفيديو: {ex.Message}");
        }
    }

    #region Helper Methods

    private static bool IsAllowedExtension(IFormFile file, string[] allowedExtensions)
    {
        var ext = Path.GetExtension(file.FileName);
        return !string.IsNullOrEmpty(ext) &&
               allowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }

    private string GetWebRootPath() =>
        !string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? _env.WebRootPath
            : Path.Combine(_env.ContentRootPath, "wwwroot");

    private async Task<string> SaveMediaFileAsync(IFormFile file, string relativeFolder)
    {
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        var webRoot = GetWebRootPath();
        var uploadPath = Path.Combine(webRoot, relativeFolder);

        if (!Directory.Exists(uploadPath))
            Directory.CreateDirectory(uploadPath);

        var filePath = Path.Combine(uploadPath, fileName);
        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var baseUrl = (_config["ImageSettings:BaseUrl"] ?? string.Empty).TrimEnd('/');
        var urlFolder = relativeFolder.Replace(Path.DirectorySeparatorChar, '/').Trim('/');
        return $"{baseUrl}/{urlFolder}/{fileName}";
    }

    private void DeleteLocalFile(string? mediaUrl)
    {
        if (string.IsNullOrWhiteSpace(mediaUrl))
            return;

        try
        {
            string relativePath;
            if (Uri.TryCreate(mediaUrl, UriKind.Absolute, out var uri))
            {
                var configuredBaseUrl = _config["ImageSettings:BaseUrl"];
                if (!string.IsNullOrWhiteSpace(configuredBaseUrl) &&
                    Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var baseUri) &&
                    !string.Equals(uri.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                relativePath = uri.AbsolutePath.TrimStart('/');
            }
            else
            {
                relativePath = mediaUrl.TrimStart('/');
            }

            if (string.IsNullOrWhiteSpace(relativePath))
                return;

            var webRoot = Path.GetFullPath(GetWebRootPath());
            var webRootWithSeparator = webRoot.EndsWith(Path.DirectorySeparatorChar)
                ? webRoot
                : webRoot + Path.DirectorySeparatorChar;

            var candidatePath = Path.GetFullPath(
                Path.Combine(webRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

            if (!candidatePath.StartsWith(webRootWithSeparator, StringComparison.OrdinalIgnoreCase))
                return;

            if (File.Exists(candidatePath))
                File.Delete(candidatePath);
        }
        catch
        {
            // Ignore cleanup errors for external or inaccessible URLs
        }
    }

    #endregion
}
