using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Astrox.Blog.Pages.Account;

public class LogoutModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(SignInManager<IdentityUser> signInManager, ILogger<LogoutModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var email = User.Identity?.Name;
        await _signInManager.SignOutAsync();
        _logger.LogInformation("已退出登录：{Email}", email);
        return RedirectToPage("/Index");
    }

    public IActionResult OnGet() => RedirectToPage("/Index");
}
