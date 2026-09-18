namespace Astrox.Blog.Models;

public class BlogOptions
{
    public const string SectionName = "Blog";

    public string Title { get; set; } = "Astrox.Blog";
    public string OwnerName { get; set; } = "Yunfei Li";
    public string Tagline { get; set; } = "航天 · 技术 · 星辰";

    /// <summary>Fixed category labels, comma-separated. Sidebar and index always show this list.</summary>
    public string Categories { get; set; } = "STK,Cesium,轨道力学,AI,Web,GIS";

    public IReadOnlyList<string> CategoryList =>
        string.IsNullOrWhiteSpace(Categories)
            ? new[] { "STK", "Cesium", "轨道力学", "AI", "Web", "GIS" }
            : Categories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    /// <summary>Footer update text, e.g. a last-updated date.</summary>
    public string Update { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    /// <summary>Plain API key from config/env. Compared with constant-time equality.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 持久化文章媒体根目录（zip 导入与 Admin/API 相对路径图片共用）。
    /// 映射 URL 前缀 <c>/media</c>；Admin/API 文章图落在 <c>{MediaRoot}/posts/{slug}/</c>。
    /// 须位于 IIS 站点目录外。空 = ContentRoot/astrox-blog-media。
    /// </summary>
    public string MediaRoot { get; set; } = string.Empty;

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
