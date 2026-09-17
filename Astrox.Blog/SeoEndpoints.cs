using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Astrox.Blog.Data;
using Astrox.Blog.Services;
using Microsoft.EntityFrameworkCore;

namespace Astrox.Blog;

public static class SeoEndpoints
{
    public static IEndpointRouteBuilder MapSeoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/robots.txt", (HttpRequest request, SiteUrlService urls) =>
        {
            var baseUrl = urls.GetBaseUrl(request);
            var body = $"""
                User-agent: *
                Allow: /
                Disallow: /Admin
                Disallow: /Account
                Disallow: /api/

                Sitemap: {baseUrl}/sitemap.xml
                """.Replace("\n", "\r\n");
            return Results.Text(body, "text/plain; charset=utf-8");
        });

        endpoints.MapGet("/sitemap.xml", async (HttpRequest request, ApplicationDbContext db, SiteUrlService urls) =>
        {
            var baseUrl = urls.GetBaseUrl(request);
            var posts = await db.Posts.AsNoTracking()
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.PublishedAt ?? p.UpdatedAt)
                .Select(p => new { p.Slug, p.UpdatedAt, p.PublishedAt })
                .ToListAsync();

            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var urlset = new XElement(ns + "urlset",
                new XElement(ns + "url",
                    new XElement(ns + "loc", baseUrl + "/"),
                    new XElement(ns + "changefreq", "daily"),
                    new XElement(ns + "priority", "1.0")));

            foreach (var post in posts)
            {
                var lastmod = (post.UpdatedAt != default ? post.UpdatedAt : post.PublishedAt ?? DateTime.UtcNow)
                    .ToUniversalTime()
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                urlset.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/Posts/{Uri.EscapeDataString(post.Slug)}"),
                    new XElement(ns + "lastmod", lastmod),
                    new XElement(ns + "changefreq", "weekly"),
                    new XElement(ns + "priority", "0.8")));
            }

            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), urlset);
            using var ms = new MemoryStream();
            var settings = new System.Xml.XmlWriterSettings
            {
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                Indent = true,
                OmitXmlDeclaration = false
            };
            using (var writer = System.Xml.XmlWriter.Create(ms, settings))
                doc.Save(writer);

            return Results.Bytes(ms.ToArray(), "application/xml; charset=utf-8");
        });

        return endpoints;
    }
}
