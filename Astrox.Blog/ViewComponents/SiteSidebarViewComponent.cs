using Astrox.Blog.Data;
using Astrox.Blog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Astrox.Blog.ViewComponents;

public class SiteSidebarViewComponent : ViewComponent
{
    public const int LatestCommentCount = 8;

    private readonly ApplicationDbContext _db;

    public SiteSidebarViewComponent(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var published = await _db.Posts.AsNoTracking()
            .Where(p => p.IsPublished)
            .Select(p => p.Tags)
            .ToListAsync();

        var categories = published
            .SelectMany(tags => string.IsNullOrWhiteSpace(tags)
                ? Enumerable.Empty<string>()
                : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SidebarCategory(g.First(), g.Count()))
            .OrderByDescending(c => c.Count)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
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

        return View(new SiteSidebarModel
        {
            Toc = toc,
            Categories = categories,
            ActiveTag = activeTag,
            LatestComments = comments
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
}

public sealed record SidebarCategory(string Name, int Count);

public sealed record SidebarComment(
    string AuthorName,
    string Excerpt,
    string PostTitle,
    string PostSlug,
    DateTime CreatedAt);
