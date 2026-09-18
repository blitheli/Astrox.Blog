using System.ComponentModel.DataAnnotations;

namespace Astrox.Blog.Models;

public class Post
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(220)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Summary { get; set; }

    [Required]
    public string Markdown { get; set; } = string.Empty;

    /// <summary>Comma-separated tag labels, e.g. "航天,技术".</summary>
    [MaxLength(400)]
    public string? Tags { get; set; }

    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PublishedAt { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public IEnumerable<string> TagList =>
        string.IsNullOrWhiteSpace(Tags)
            ? Enumerable.Empty<string>()
            : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
