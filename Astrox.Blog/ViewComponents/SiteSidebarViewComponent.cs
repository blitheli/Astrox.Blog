using Astrox.Blog.Data;
using Astrox.Blog.Models;
using Astrox.Blog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Astrox.Blog.ViewComponents;

public class SiteSidebarViewComponent : ViewComponent
{
    public const int LatestCommentCount = 8;

    private readonly ApplicationDbContext _db;
    private readonly BlogOptions _options;
    private readonly PageViewService _pageViews;

    public SiteSidebarViewComponent(
        ApplicationDbContext db,
        IOptions<BlogOptions> options,
        PageViewService pageViews)
    {
        _db = db;
        _options = options.Value;
        _pageViews = pageViews;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var published = await _db.Posts.AsNoTracking()
            .Where(p => p.IsPublished)
            .Select(p => p.Tags)
            .ToListAsync();

        var counts = published
            .SelectMany(tags => string.IsNullOrWhiteSpace(tags)
                ? Enumerable.Empty<string>()
                : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var categories = _options.CategoryList
            .Select(name => new SidebarCategory(name, counts.TryGetValue(name, out var n) ? n : 0))
            .ToList();

        var comments = await _db.Comments.AsNoTracking()
            .Where(c => !c.IsDeleted && c.Post!.IsPublished)
            .OrderByDescending(c => c.CreatedAt)
            .Take(LatestCommentCount)
            .Select(c => new SidebarComment(
                c.AuthorName,
                Truncate(c.Body, 42),
                c.Post!.Title,
                c.Post.Slug,
                c.CreatedAt))
            .ToListAsync();

        var toc = ViewData["ArticleToc"] as IReadOnlyList<TocEntry> ?? Array.Empty<TocEntry>();
        var activeTag = HttpContext.Request.Query["tag"].ToString();
        if (string.IsNullOrWhiteSpace(activeTag))
            activeTag = null;

        var totalPv = await _pageViews.GetTotalPvAsync();

        return View(new SiteSidebarModel
        {
            Toc = toc,
            Categories = categories,
            ActiveTag = activeTag,
            LatestComments = comments,
            TotalPv = totalPv
        });
    }

    private static string Truncate(string text, int max)
    {
        var trimmed = text.Trim().ReplaceLineEndings(" ");
        return trimmed.Length <= max ? trimmed : trimmed[..max].TrimEnd() + "…";
    }
}

public sealed class SiteSidebarModel
{
    public IReadOnlyList<TocEntry> Toc { get; init; } = Array.Empty<TocEntry>();
    public IReadOnlyList<SidebarCategory> Categories { get; init; } = Array.Empty<SidebarCategory>();
    public string? ActiveTag { get; init; }
    public IReadOnlyList<SidebarComment> LatestComments { get; init; } = Array.Empty<SidebarComment>();
    public long TotalPv { get; init; }
}

public sealed record SidebarCategory(string Name, int Count);

public sealed record SidebarComment(
    string AuthorName,
    string Excerpt,
    string PostTitle,
    string PostSlug,
    DateTime CreatedAt);
