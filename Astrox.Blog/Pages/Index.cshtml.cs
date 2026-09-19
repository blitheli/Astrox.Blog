using Astrox.Blog.Data;
using Astrox.Blog.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Astrox.Blog.Pages;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly BlogOptions _options;

    public IndexModel(ApplicationDbContext db, IOptions<BlogOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public BlogOptions Blog => _options;
    public string? ActiveTag { get; private set; }
    public IList<PostCard> Posts { get; private set; } = new List<PostCard>();
    public IList<string> AllTags { get; private set; } = new List<string>();

    public async Task OnGetAsync(string? tag)
    {
        ActiveTag = string.IsNullOrWhiteSpace(tag) ? null : tag.Trim();

        var published = await _db.Posts.AsNoTracking()
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .ToListAsync();

        AllTags = _options.CategoryList.ToList();

        if (ActiveTag is not null)
        {
            published = published
                .Where(p => p.TagList.Contains(ActiveTag, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        var ids = published.Select(p => p.Id).ToList();
        var views = await _db.PostViewCounts.AsNoTracking()
            .Where(v => ids.Contains(v.PostId))
            .ToDictionaryAsync(v => v.PostId, v => v.Count);
        var comments = await _db.Comments.AsNoTracking()
            .Where(c => ids.Contains(c.PostId) && !c.IsDeleted)
            .GroupBy(c => c.PostId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        Posts = published
            .Select(p => new PostCard(
                p,
                views.GetValueOrDefault(p.Id),
                comments.GetValueOrDefault(p.Id)))
            .ToList();
    }
}

public sealed record PostCard(Post Post, long ViewCount, int CommentCount);
