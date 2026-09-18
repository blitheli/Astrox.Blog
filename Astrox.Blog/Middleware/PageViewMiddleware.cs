using Astrox.Blog.Services;

namespace Astrox.Blog.Middleware;

/// <summary>
/// 公开 GET 成功响应后异步累加站内访问计数；不阻塞页面主体写出。
/// </summary>
public sealed class PageViewMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PageViewMiddleware> _logger;

    public PageViewMiddleware(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory,
        ILogger<PageViewMiddleware> logger)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (!ShouldCount(context))
            return;

        var path = context.Request.Path.Value ?? "/";
        var query = context.Request.QueryString.HasValue
            ? context.Request.QueryString.Value!
            : string.Empty;
        var urlKey = path + query;
        var ip = context.Connection.RemoteIpAddress?.ToString();
        var ua = context.Request.Headers.UserAgent.ToString();
        var slug = TryExtractPostSlug(path);

        // 不 await：避免拖慢响应收尾
        _ = RecordInBackgroundAsync(urlKey, ip, ua, slug);
    }

    private async Task RecordInBackgroundAsync(
        string urlKey,
        string? ip,
        string? userAgent,
        string? postSlug)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<PageViewService>();
            await service.TryRecordAsync(urlKey, ip, userAgent, postSlug);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "页面访问计数后台任务失败");
        }
    }

    internal static bool ShouldCount(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
            return false;

        var status = context.Response.StatusCode;
        if (status is < 200 or >= 300)
            return false;

        var path = context.Request.Path.Value ?? "/";
        if (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/Account", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/Error", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "/robots.txt", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "/sitemap.xml", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // 仅统计公开 Razor 页：首页与文章详情
        if (path == "/" || path.Equals("/Index", StringComparison.OrdinalIgnoreCase))
            return true;

        return path.StartsWith("/Posts/", StringComparison.OrdinalIgnoreCase);
    }

    internal static string? TryExtractPostSlug(string path)
    {
        const string prefix = "/Posts/";
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var rest = path[prefix.Length..].Trim('/');
        if (string.IsNullOrWhiteSpace(rest))
            return null;

        var slash = rest.IndexOf('/');
        return slash < 0 ? rest : rest[..slash];
    }
}
