using System.ComponentModel.DataAnnotations;

namespace Astrox.Blog.Models;

public class Comment
{
    public int Id { get; set; }

    public int PostId { get; set; }

    public Post? Post { get; set; }

    [Required, MaxLength(64)]
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>可选；仅存库，前台不公开显示。</summary>
    [MaxLength(200)]
    public string? AuthorEmail { get; set; }

    [Required, MaxLength(2000)]
    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>IP 的单向哈希，不存明文。</summary>
    [MaxLength(64)]
    public string IpHash { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? UserAgent { get; set; }

    /// <summary>软删；前台只显示未删除评论。</summary>
    public bool IsDeleted { get; set; }
}
