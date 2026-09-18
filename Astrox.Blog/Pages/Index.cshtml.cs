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
    public IList<Post> Posts { get; private set; } = new List<Post>();
    public IList<string> AllTags { get; private set; } = new List<string>();

    public async Task OnGetAsync(string? tag)
    {
        ActiveTag = string.IsNullOrWhiteSpace(tag) ? null : tag.Trim();

        var query = _db.Posts.AsNoTracking().Where(p => p.IsPublished);

        var published = await query
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .ToListAsync();

        AllTags = _options.CategoryList.ToList();

        if (ActiveTag is not null)
        {
            Posts = published
                .Where(p => p.TagList.Contains(ActiveTag, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }
        else
        {
            Posts = published;
        }
    }
}
