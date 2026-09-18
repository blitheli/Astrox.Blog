using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

namespace Astrox.Blog.Services;

public sealed class CommentAntiSpamService
{
    public const int MaxBodyLength = 2000;
    public const int MaxAuthorNameLength = 64;
    public const int MaxAuthorEmailLength = 200;
    public const int MaxLinks = 3;
    public const int MinFillSeconds = 3;
    public const int MaxPerMinute = 3;
    public const int MaxPerHour = 20;

    private static readonly Regex UrlRegex = new(
        @"https?://|www\.",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IMemoryCache _cache;
    private readonly byte[] _ipSalt;

    public CommentAntiSpamService(IMemoryCache cache, IConfiguration configuration)
    {
        _cache = cache;
        // 盐用于 IpHash；优先 ApiKey，否则固定开发盐（仅影响哈希，非认证）
        var saltSource = configuration["Blog:ApiKey"];
        if (string.IsNullOrWhiteSpace(saltSource))
            saltSource = "astrox-blog-comment-ip-salt";
        _ipSalt = Encoding.UTF8.GetBytes(saltSource);
    }

    public string IssueFormToken()
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        _cache.Set(TokenKey(token), DateTimeOffset.UtcNow, TimeSpan.FromHours(2));
        return token;
    }

    public string HashIp(string? ip)
    {
        var value = string.IsNullOrWhiteSpace(ip) ? "unknown" : ip.Trim();
        var bytes = Encoding.UTF8.GetBytes(value);
        var payload = new byte[_ipSalt.Length + bytes.Length];
        Buffer.BlockCopy(_ipSalt, 0, payload, 0, _ipSalt.Length);
        Buffer.BlockCopy(bytes, 0, payload, _ipSalt.Length, bytes.Length);
        var hash = SHA256.HashData(payload);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public string? TruncateUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return null;
        var ua = userAgent.Trim();
        return ua.Length <= 300 ? ua : ua[..300];
    }

    public CommentValidationResult ValidateSubmission(
        string? honeypot,
        string? formToken,
        string? authorName,
        string? authorEmail,
        string? body,
        string ipHash)
    {
        // 蜜罐：有填写则静默丢弃（对机器人返回“成功”语义由调用方处理）
        if (!string.IsNullOrWhiteSpace(honeypot))
            return CommentValidationResult.Honeypot();

        if (string.IsNullOrWhiteSpace(formToken)
            || !_cache.TryGetValue(TokenKey(formToken), out DateTimeOffset issuedAt))
        {
            return CommentValidationResult.Fail("表单已过期，请刷新页面后重试。");
        }

        var elapsed = DateTimeOffset.UtcNow - issuedAt;
        if (elapsed < TimeSpan.FromSeconds(MinFillSeconds))
            return CommentValidationResult.Fail("提交过快，请稍后再试。");

        // 一次性 token，防止重放
        _cache.Remove(TokenKey(formToken));

        var name = (authorName ?? string.Empty).Trim();
        if (name.Length is < 1 or > MaxAuthorNameLength)
            return CommentValidationResult.Fail($"昵称长度需在 1–{MaxAuthorNameLength} 字之间。");

        string? email = null;
        if (!string.IsNullOrWhiteSpace(authorEmail))
        {
            email = authorEmail.Trim();
            if (email.Length > MaxAuthorEmailLength)
                return CommentValidationResult.Fail("邮箱过长。");
            if (!email.Contains('@') || email.Contains(' '))
                return CommentValidationResult.Fail("邮箱格式不正确。");
        }

        var text = (body ?? string.Empty).Trim();
        if (text.Length is < 2 or > MaxBodyLength)
            return CommentValidationResult.Fail($"评论正文需在 2–{MaxBodyLength} 字之间。");

        if (IsMostlyRepeated(text))
            return CommentValidationResult.Fail("评论内容无效（疑似重复字符）。");

        var linkCount = UrlRegex.Matches(text).Count;
        if (linkCount > MaxLinks)
            return CommentValidationResult.Fail($"外链过多（最多 {MaxLinks} 个）。");

        if (!TryConsumeRateLimit(ipHash, out var rateMessage))
            return CommentValidationResult.Fail(rateMessage!);

        return CommentValidationResult.Ok(name, email, text);
    }

    private bool TryConsumeRateLimit(string ipHash, out string? message)
    {
        message = null;
        var now = DateTimeOffset.UtcNow;
        var minuteKey = $"comment-rate:{ipHash}:m";
        var hourKey = $"comment-rate:{ipHash}:h";

        // 可变计数器对象，避免每次 Set 重置绝对过期窗口
        var minute = _cache.GetOrCreate(minuteKey, e =>
        {
            e.AbsoluteExpiration = now.AddMinutes(1);
            return new RateCounter();
        })!;
        var hour = _cache.GetOrCreate(hourKey, e =>
        {
            e.AbsoluteExpiration = now.AddHours(1);
            return new RateCounter();
        })!;

        if (minute.Count >= MaxPerMinute)
        {
            message = "评论过于频繁，请稍后再试（每分钟最多 3 条）。";
            return false;
        }

        if (hour.Count >= MaxPerHour)
        {
            message = "评论过于频繁，请稍后再试（每小时最多 20 条）。";
            return false;
        }

        minute.Count++;
        hour.Count++;
        return true;
    }

    private sealed class RateCounter
    {
        public int Count;
    }

    private static bool IsMostlyRepeated(string text)
    {
        if (text.Length < 8)
            return false;
        var distinct = text.Distinct().Count();
        return distinct <= 2;
    }

    private static string TokenKey(string token) => $"comment-form-token:{token}";
}

public sealed class CommentValidationResult
{
    public bool Succeeded { get; private init; }
    public bool IsHoneypot { get; private init; }
    public string? ErrorMessage { get; private init; }
    public string AuthorName { get; private init; } = string.Empty;
    public string? AuthorEmail { get; private init; }
    public string Body { get; private init; } = string.Empty;

    public static CommentValidationResult Ok(string name, string? email, string body) => new()
    {
        Succeeded = true,
        AuthorName = name,
        AuthorEmail = email,
        Body = body
    };

    public static CommentValidationResult Fail(string message) => new()
    {
        Succeeded = false,
        ErrorMessage = message
    };

    public static CommentValidationResult Honeypot() => new()
    {
        Succeeded = false,
        IsHoneypot = true
    };
}
