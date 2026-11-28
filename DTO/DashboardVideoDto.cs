namespace WallsShop.DTO.Dashboard;

public class DashboardVideoDto
{
    public int Id { get; set; }
    public string VideoUrl { get; set; } = string.Empty;
    public string CoverImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUpdateVideoDto
{
    public bool? IsActive { get; set; }
    public int? DisplayOrder { get; set; }
    public IFormFile? VideoFile { get; set; }
    public string? VideoUrlLink { get; set; }
    public IFormFile? CoverImageFile { get; set; }
    public string? CoverImageUrlLink { get; set; }
}
