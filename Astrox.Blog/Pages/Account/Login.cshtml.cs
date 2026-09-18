using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Astrox.Blog.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(SignInManager<IdentityUser> signInManager, ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "请输入邮箱")]
        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        [Display(Name = "邮箱")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "请输入密码")]
        [DataType(DataType.Password)]
        [Display(Name = "密码")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "记住我")]
        public bool RememberMe { get; set; }
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/Admin");
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/Admin");
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return Page();

        var result = await _signInManager.PasswordSignInAsync(
            Input.Email.Trim(),
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("登录成功：{Email}", Input.Email.Trim());
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
            _logger.LogWarning("登录被锁定：{Email}", Input.Email.Trim());
        else
            _logger.LogWarning("登录失败：{Email}", Input.Email.Trim());

        ModelState.AddModelError(string.Empty, "邮箱或密码不正确。");
        return Page();
    }
}
