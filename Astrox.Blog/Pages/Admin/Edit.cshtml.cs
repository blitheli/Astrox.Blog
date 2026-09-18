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
[RequestSizeLimit(52_428_800)] // 50 MB：正文 + 多图
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly MarkdownService _markdown;
    private readonly PostMediaService _media;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        ApplicationDbContext db,
        MarkdownService markdown,
        PostMediaService media,
        ILogger<EditModel> logger)
    {
        _db = db;
        _markdown = markdown;
        _media = media;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public List<IFormFile>? UploadImages { get; set; }

    [BindProperty]
    public IFormFile? InsertImage { get; set; }

    public bool IsNew => Input.Id is null or 0;

    public string PreviewHtml { get; private set; } = string.Empty;

    public IReadOnlyList<string> ImageMessages { get; private set; } = Array.Empty<string>();

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
        if (!ModelState.IsValid)
        {
            PreviewHtml = _markdown.ToHtml(Input.Markdown);
            return Page();
        }

        var slug = SoftNormalizeSlug(Input.Slug, Input.Title);
        var markdown = await ProcessImagesAsync(slug, Input.Markdown);
        Input.Markdown = markdown;
        PreviewHtml = _markdown.ToHtml(markdown);

        Post post;

        if (Input.Id is null or 0)
        {
            if (await _db.Posts.AnyAsync(p => p.Slug == slug))
            {
                ModelState.AddModelError("Input.Slug", "该 slug 已被占用");
                return Page();
            }

            var now = DateTime.UtcNow;
            post = new Post
            {
                Title = Input.Title.Trim(),
                Slug = slug,
                Summary = NullIfEmpty(Input.Summary),
                Tags = NullIfEmpty(Input.Tags),
                Markdown = markdown,
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
            post.Markdown = markdown;
            post.IsPublished = Input.IsPublished;
            post.UpdatedAt = DateTime.UtcNow;
            if (Input.IsPublished && !wasPublished)
                post.PublishedAt = DateTime.UtcNow;
            else if (!Input.IsPublished)
                post.PublishedAt = null;
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation(
            "{Action}文章 {Id} / {Slug}，发布={Published}",
            Input.Id is null or 0 ? "新建" : "更新",
            post.Id,
            post.Slug,
            post.IsPublished);
        return RedirectToPage("/Admin/Index");
    }

    public IActionResult OnPostPreview()
    {
        PreviewHtml = _markdown.ToHtml(Input.Markdown);
        return Page();
    }

    /// <summary>上传单张图片并插入 Markdown 链接（不保存文章）。</summary>
    public async Task<IActionResult> OnPostUploadImageAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.Title) && string.IsNullOrWhiteSpace(Input.Slug))
        {
            ModelState.AddModelError(string.Empty, "请先填写标题或 Slug，以便确定图片存放目录。");
            PreviewHtml = _markdown.ToHtml(Input.Markdown);
            return Page();
        }

        if (InsertImage is null || InsertImage.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "请选择要上传的图片文件。");
            PreviewHtml = _markdown.ToHtml(Input.Markdown);
            return Page();
        }

        var slug = SoftNormalizeSlug(Input.Slug, string.IsNullOrWhiteSpace(Input.Title) ? "post" : Input.Title);
        try
        {
            var url = await _media.SaveUploadAsync(slug, InsertImage);
            var alt = Path.GetFileNameWithoutExtension(InsertImage.FileName);
            var snippet = $"![{alt}]({url})";
            Input.Markdown = string.IsNullOrWhiteSpace(Input.Markdown)
                ? snippet
                : Input.Markdown.TrimEnd() + "\n\n" + snippet + "\n";
            Input.Slug ??= slug;
            ImageMessages = new[] { $"已上传并插入：{url}" };
            _logger.LogInformation("Admin 上传图片并插入 {Url}（slug={Slug}）", url, slug);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            _logger.LogWarning(ex, "Admin 上传图片失败");
        }

        PreviewHtml = _markdown.ToHtml(Input.Markdown);
        return Page();
    }

    private async Task<string> ProcessImagesAsync(string slug, string markdown)
    {
        var files = new List<IFormFile>();
        if (UploadImages is { Count: > 0 })
            files.AddRange(UploadImages.Where(f => f.Length > 0));
        if (InsertImage is { Length: > 0 })
            files.Add(InsertImage);

        // 先落盘所有随表单上传的文件（按文件名），再扫描改写
        foreach (var file in files)
        {
            try
            {
                await _media.SaveUploadAsync(slug, file);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "保存随表单上传的图片失败：{Name}", file.FileName);
                ModelState.AddModelError(string.Empty, $"上传失败 {file.FileName}：{ex.Message}");
            }
        }

        var uploads = PostMediaService.IndexFormFiles(files);
        var result = _media.ProcessMarkdown(markdown, slug, uploads);
        var messages = new List<string>();
        messages.AddRange(result.Rewritten.Select(x => $"已托管：{x}"));
        messages.AddRange(result.Warnings);
        ImageMessages = messages;

        foreach (var w in result.Warnings)
            _logger.LogWarning("Markdown 图片处理：{Warning}", w);

        return result.Markdown;
    }

    private static string SoftNormalizeSlug(string? slug, string title) =>
        SlugHelper.Normalize(slug, title);

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
