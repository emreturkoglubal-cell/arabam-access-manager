using AccessManager.Application.Interfaces;
using AccessManager.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.UI.Pages.Account;

[AllowAnonymous]
public class LogoutModel : PageModel
{
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public LogoutModel(ICurrentUserService currentUser, IAuditService auditService)
    {
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (_currentUser.IsAuthenticated)
        {
            var userId = _currentUser.UserId;
            var displayName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            _auditService.Log(AuditAction.Logout, userId, displayName, "AppUser", userId?.ToString(), "Çıkış yapıldı", ip);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Account/Login");
    }
}
