using System.ComponentModel.DataAnnotations;
using Astrox.Blog.Data;
using Astrox.Blog.Models;
using Astrox.Blog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Astrox.Blog.Pages.Admin;

[Authorize]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly MarkdownService _markdown;

    public EditModel(ApplicationDbContext db, MarkdownService markdown)
    {
        _db = db;
        _markdown = markdown;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool IsNew => Input.Id is null or 0;

    public string PreviewHtml { get; private set; } = string.Empty;

    public class InputModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "请填写标题")]
        [MaxLength(200)]
        [Display(Name = "标题")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(220)]
        [Display(Name = "Slug")]
        public string? Slug { get; set; }

        [MaxLength(500)]
        [Display(Name = "摘要")]
        public string? Summary { get; set; }

        [MaxLength(400)]
        [Display(Name = "标签")]
        public string? Tags { get; set; }

        [Required(ErrorMessage = "请填写 Markdown 正文")]
        [Display(Name = "Markdown")]
        public string Markdown { get; set; } = string.Empty;

        [Display(Name = "发布")]
        public bool IsPublished { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
            return Page();

        var post = await _db.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (post is null)
            return NotFound();

        Input = new InputModel
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Summary = post.Summary,
            Tags = post.Tags,
            Markdown = post.Markdown,
            IsPublished = post.IsPublished
        };
        PreviewHtml = _markdown.ToHtml(post.Markdown);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        PreviewHtml = _markdown.ToHtml(Input.Markdown);

        if (!ModelState.IsValid)
            return Page();

        var slug = SoftNormalizeSlug(Input.Slug, Input.Title);
        Post post;

        if (Input.Id is null or 0)
        {
            if (await _db.Posts.AnyAsync(p => p.Slug == slug))
            {
                ModelState.AddModelError("Input.Slug", "该 slug 已被占用");
                return Page();
            }

            var now = DateTimeOffset.UtcNow;
            post = new Post
            {
                Title = Input.Title.Trim(),
                Slug = slug,
                Summary = NullIfEmpty(Input.Summary),
                Tags = NullIfEmpty(Input.Tags),
                Markdown = Input.Markdown,
                IsPublished = Input.IsPublished,
                CreatedAt = now,
                UpdatedAt = now,
                PublishedAt = Input.IsPublished ? now : null
            };
            _db.Posts.Add(post);
        }
        else
        {
            var existing = await _db.Posts.FirstOrDefaultAsync(p => p.Id == Input.Id);
            if (existing is null)
                return NotFound();
            post = existing;

            if (slug != post.Slug && await _db.Posts.AnyAsync(p => p.Slug == slug))
            {
                ModelState.AddModelError("Input.Slug", "该 slug 已被占用");
                return Page();
            }

            var wasPublished = post.IsPublished;
            post.Title = Input.Title.Trim();
            post.Slug = slug;
            post.Summary = NullIfEmpty(Input.Summary);
            post.Tags = NullIfEmpty(Input.Tags);
            post.Markdown = Input.Markdown;
            post.IsPublished = Input.IsPublished;
            post.UpdatedAt = DateTimeOffset.UtcNow;
            if (Input.IsPublished && !wasPublished)
                post.PublishedAt = DateTimeOffset.UtcNow;
            else if (!Input.IsPublished)
                post.PublishedAt = null;
        }

        await _db.SaveChangesAsync();
        return RedirectToPage("/Admin/Index");
    }

    public IActionResult OnPostPreview()
    {
        PreviewHtml = _markdown.ToHtml(Input.Markdown);
        return Page();
    }

    private static string SoftNormalizeSlug(string? slug, string title) =>
        SlugHelper.Normalize(slug, title);

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
