using Astrox.Blog.Data;
using Astrox.Blog.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Astrox.Blog.Pages.Admin;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public IList<Post> Posts { get; private set; } = new List<Post>();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        Posts = await _db.Posts
            .AsNoTracking()
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post is not null)
        {
            _db.Posts.Remove(post);
            await _db.SaveChangesAsync();
            StatusMessage = $"已删除「{post.Title}」";
        }
        return RedirectToPage();
    }
}
