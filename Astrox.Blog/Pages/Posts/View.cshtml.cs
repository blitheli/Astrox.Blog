using Astrox.Blog.Data;
using Astrox.Blog.Models;
using Astrox.Blog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Astrox.Blog.Pages.Posts;

public class ViewModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly MarkdownService _markdown;

    public ViewModel(ApplicationDbContext db, MarkdownService markdown)
    {
        _db = db;
        _markdown = markdown;
    }

    public Post Post { get; private set; } = null!;
    public string HtmlContent { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var post = await _db.Posts.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug);

        if (post is null)
            return NotFound();

        var isOwner = User.Identity?.IsAuthenticated == true;
        if (!post.IsPublished && !isOwner)
            return NotFound();

        Post = post;
        HtmlContent = _markdown.ToHtml(post.Markdown);
        return Page();
    }
}
