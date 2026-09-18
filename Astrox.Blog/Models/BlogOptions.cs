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

    /// <summary>Public site origin, e.g. https://blog.example.com (no trailing slash). Used for canonical / OG / sitemap.</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    /// <summary>Umami tracker script URL, e.g. https://analytics.example.com/script.js</summary>
    public string UmamiScriptUrl { get; set; } = string.Empty;

    /// <summary>Umami website id (data-website-id).</summary>
    public string UmamiWebsiteId { get; set; } = string.Empty;

    /// <summary>Optional raw HTML injected into &lt;head&gt; (e.g. extra analytics). Empty = skip.</summary>
    public string ExtraHeadSnippet { get; set; } = string.Empty;

    public bool IsUmamiConfigured =>
        !string.IsNullOrWhiteSpace(UmamiScriptUrl) && !string.IsNullOrWhiteSpace(UmamiWebsiteId);

    public bool IsExtraHeadConfigured => !string.IsNullOrWhiteSpace(ExtraHeadSnippet);
}
