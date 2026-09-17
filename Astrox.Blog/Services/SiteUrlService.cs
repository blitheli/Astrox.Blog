using Astrox.Blog.Models;
using Microsoft.Extensions.Options;

namespace Astrox.Blog.Services;

public class SiteUrlService
{
    private readonly BlogOptions _options;

    public SiteUrlService(IOptions<BlogOptions> options)
    {
        _options = options.Value;
    }

    public string GetBaseUrl(HttpRequest request)
    {
        var configured = _options.PublicBaseUrl?.Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        return $"{request.Scheme}://{request.Host.Value}".TrimEnd('/');
    }

    public string Absolute(HttpRequest request, string pathAndQuery)
    {
        var baseUrl = GetBaseUrl(request);
        if (string.IsNullOrEmpty(pathAndQuery) || pathAndQuery == "/")
            return baseUrl + "/";

        if (!pathAndQuery.StartsWith('/'))
            pathAndQuery = "/" + pathAndQuery;

        return baseUrl + pathAndQuery;
    }
}
