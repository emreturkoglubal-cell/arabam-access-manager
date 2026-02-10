using System.Security.Claims;
using AccessManager.Application.Interfaces;
using AccessManager.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpClientFactory _httpClientFactory;

    public AccountController(IAuthService authService, IAuditService auditService, ICurrentUserService currentUser, IHttpClientFactory httpClientFactory)
    {
        _authService = authService;
        _auditService = auditService;
        _currentUser = currentUser;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl ?? Url.Content("~/");
        return View(new LoginInputModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInputModel model, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        returnUrl ??= Url.Content("~/");

        if (string.IsNullOrWhiteSpace(model.UserName) || string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(string.Empty, "Kullanıcı adı ve parola gerekli.");
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var user = _authService.ValidateUser(model.UserName, model.Password);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı adı veya parola.");
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
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
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        _auditService.Log(AuditAction.Login, user.Id, user.DisplayName, "AppUser", user.Id.ToString(), "Giriş yapıldı", ip);

        return LocalRedirect(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Account/Logout")]
    public async Task<IActionResult> Logout()
    {
        if (_currentUser.IsAuthenticated)
        {
            var userId = _currentUser.UserId;
            var displayName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            _auditService.Log(AuditAction.Logout, userId, displayName, "AppUser", userId?.ToString(), "Çıkış yapıldı", ip);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    /// <summary>
    /// Giriş sayfasında yüz doğrulama için personel fotoğrafı. Same-origin ile CORS olmadan kullanılır.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> PersonnelPhoto(string? username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            return NotFound();

        var photoUrl = _authService.GetPersonnelPhotoUrlByUsername(username);
        if (string.IsNullOrWhiteSpace(photoUrl))
            return NotFound();

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var response = await client.GetAsync(photoUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return NotFound();

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            return File(bytes, contentType);
        }
        catch
        {
            return NotFound();
        }
    }
}

public class LoginInputModel
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
    public bool UseFaceVerification { get; set; }
}
