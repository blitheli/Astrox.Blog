using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Astrox.Blog.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Astrox.Blog.Services;

public static class ApiKeyAuthDefaults
{
    public const string Scheme = "ApiKey";
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly BlogOptions _blogOptions;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<BlogOptions> blogOptions)
        : base(options, logger, encoder)
    {
        _blogOptions = blogOptions.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var configured = _blogOptions.ApiKey;
        if (string.IsNullOrWhiteSpace(configured))
            return Task.FromResult(AuthenticateResult.Fail("API Key 未配置"));

        if (!Request.Headers.TryGetValue("Authorization", out var header))
            return Task.FromResult(AuthenticateResult.NoResult());

        var value = header.ToString();
        if (!value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        var provided = value["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(provided) || !FixedTimeEquals(provided, configured))
        {
            Logger.LogWarning("API Key 认证失败，来自 {RemoteIp}", Context.Connection.RemoteIpAddress);
            return Task.FromResult(AuthenticateResult.Fail("无效的 API Key"));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "api-publisher"),
            new Claim("auth_method", "api_key")
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}
