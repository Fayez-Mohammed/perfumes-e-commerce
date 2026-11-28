using System.ComponentModel.DataAnnotations;

namespace WallsShop.Entity;

public class Video
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string VideoUrl { get; set; } = string.Empty;

    [Required]
    public string CoverImageUrl { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
