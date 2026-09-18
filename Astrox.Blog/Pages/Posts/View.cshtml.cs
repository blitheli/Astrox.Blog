using System.ComponentModel.DataAnnotations;
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
    private readonly CommentAntiSpamService _antiSpam;
    private readonly ILogger<ViewModel> _logger;

    public ViewModel(
        ApplicationDbContext db,
        MarkdownService markdown,
        CommentAntiSpamService antiSpam,
        ILogger<ViewModel> logger)
    {
        _db = db;
        _markdown = markdown;
        _antiSpam = antiSpam;
        _logger = logger;
    }

    public Post Post { get; private set; } = null!;
    public string HtmlContent { get; private set; } = string.Empty;
    public IList<Comment> Comments { get; private set; } = new List<Comment>();
    public bool IsOwner => User.Identity?.IsAuthenticated == true;

    [BindProperty]
    public CommentInput Input { get; set; } = new();

    [TempData]
    public string? CommentStatus { get; set; }

    [TempData]
    public string? CommentError { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var post = await _db.Posts.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug);

        if (post is null)
            return NotFound();

        if (!post.IsPublished && !IsOwner)
            return NotFound();

        Post = post;
        HtmlContent = _markdown.ToHtml(post.Markdown);
        await LoadCommentsAsync(post.Id);
        PrepareFormToken();
        return Page();
    }

    public async Task<IActionResult> OnPostCommentAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Slug == slug);
        if (post is null)
            return NotFound();

        if (!post.IsPublished)
        {
            CommentError = "草稿文章暂不开放评论。";
            return RedirectToPage(new { slug });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ipHash = _antiSpam.HashIp(ip);
        var result = _antiSpam.ValidateSubmission(
            Input.Website,
            Input.FormToken,
            Input.AuthorName,
            Input.AuthorEmail,
            Input.Body,
            ipHash);

        if (result.IsHoneypot)
        {
            // 静默“成功”，不写入数据库
            _logger.LogWarning("评论蜜罐触发，已丢弃。IpHash={IpHash}", ipHash);
            CommentStatus = "评论已发布。";
            return RedirectToPage(new { slug });
        }

        if (!result.Succeeded)
        {
            CommentError = result.ErrorMessage;
            return RedirectToPage(new { slug });
        }

        var comment = new Comment
        {
            PostId = post.Id,
            AuthorName = result.AuthorName,
            AuthorEmail = result.AuthorEmail,
            Body = result.Body,
            CreatedAt = DateTime.UtcNow,
            IpHash = ipHash,
            UserAgent = _antiSpam.TruncateUserAgent(Request.Headers.UserAgent.ToString()),
            IsDeleted = false
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();
        _logger.LogInformation(
            "新评论 PostId={PostId} CommentId={CommentId} IpHash={IpHash}",
            post.Id, comment.Id, ipHash);

        CommentStatus = "评论已发布。";
        return RedirectToPage(new { slug });
    }

    public async Task<IActionResult> OnPostDeleteCommentAsync(string slug, int commentId)
    {
        if (!IsOwner)
            return Forbid();

        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var post = await _db.Posts.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug);
        if (post is null)
            return NotFound();

        var comment = await _db.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.PostId == post.Id);
        if (comment is not null && !comment.IsDeleted)
        {
            comment.IsDeleted = true;
            await _db.SaveChangesAsync();
            CommentStatus = "已删除该评论。";
            _logger.LogInformation("软删评论 CommentId={CommentId} PostId={PostId}", commentId, post.Id);
        }

        return RedirectToPage(new { slug });
    }

    private async Task LoadCommentsAsync(int postId)
    {
        Comments = await _db.Comments.AsNoTracking()
            .Where(c => c.PostId == postId && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    private void PrepareFormToken()
    {
        Input.FormToken = _antiSpam.IssueFormToken();
    }

    public class CommentInput
    {
        [Display(Name = "昵称")]
        [Required(ErrorMessage = "请填写昵称")]
        [StringLength(64, MinimumLength = 1, ErrorMessage = "昵称长度需在 1–64 字之间")]
        public string AuthorName { get; set; } = string.Empty;

        [Display(Name = "邮箱（可选，不公开）")]
        [StringLength(200)]
        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        public string? AuthorEmail { get; set; }

        [Display(Name = "评论")]
        [Required(ErrorMessage = "请填写评论内容")]
        [StringLength(2000, MinimumLength = 2, ErrorMessage = "评论正文需在 2–2000 字之间")]
        public string Body { get; set; } = string.Empty;

        /// <summary>蜜罐字段：对真人应保持为空。</summary>
        public string? Website { get; set; }

        public string? FormToken { get; set; }
    }
}
