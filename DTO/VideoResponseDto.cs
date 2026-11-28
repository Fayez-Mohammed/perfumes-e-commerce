namespace WallsShop.DTO;

public class VideoResponseDto
{
    public int Id { get; set; }
    public string VideoUrl { get; set; } = string.Empty;
    public string CoverImageUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}
