using System.Security.Claims;
using AccessManager.Application.Interfaces;
using AccessManager.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.UI.Pages.Account;

[AllowAnonymous]
[IgnoreAntiforgeryToken(Order = 1000)]
public class LoginModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly IAuditService _auditService;

    public LoginModel(IAuthService authService, IAuditService auditService)
    {
        _authService = authService;
        _auditService = auditService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");

        if (string.IsNullOrWhiteSpace(Input.UserName) || string.IsNullOrWhiteSpace(Input.Password))
        {
            ModelState.AddModelError(string.Empty, "Kullanıcı adı ve parola gerekli.");
            return Page();
        }

        var user = _authService.ValidateUser(Input.UserName, Input.Password);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı adı veya parola.");
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.GivenName, user.DisplayName),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var props = new AuthenticationProperties
        {
            IsPersistent = Input.RememberMe,
            ExpiresUtc = Input.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        _auditService.Log(AuditAction.Login, user.Id, user.DisplayName, "AppUser", user.Id.ToString(), "Giriş yapıldı", ip);

        return LocalRedirect(ReturnUrl);
    }
}

public class InputModel
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}
