using System.Security.Claims;
using AccessManager.Application.Interfaces;
using AccessManager.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace AccessManager.UI.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? UserName => User?.Identity?.Name;

    public string? DisplayName => User?.FindFirst(ClaimTypes.GivenName)?.Value ?? UserName;

    public AppRole? Role
    {
        get
        {
            var value = User?.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<AppRole>(value, true, out var role) ? role : null;
        }
    }

    public bool IsInRole(AppRole role) => Role == role;

    public bool IsInRole(string roleName) =>
        Enum.TryParse<AppRole>(roleName, true, out var role) && IsInRole(role);
}
