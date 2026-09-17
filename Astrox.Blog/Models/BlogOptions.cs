namespace Astrox.Blog.Models;

public class BlogOptions
{
    public const string SectionName = "Blog";

    public string Title { get; set; } = "Astrox.Blog";
    public string OwnerName { get; set; } = "Yunfei Li";
    public string Tagline { get; set; } = "航天 · 技术 · 星辰";
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    /// <summary>Plain API key from config/env. Compared with constant-time equality.</summary>
    public string ApiKey { get; set; } = string.Empty;
}
