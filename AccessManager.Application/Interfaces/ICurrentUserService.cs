using AccessManager.Domain.Enums;

namespace AccessManager.Application.Interfaces;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    int? UserId { get; }
    string? UserName { get; }
    string? DisplayName { get; }
    AppRole? Role { get; }
    bool IsInRole(AppRole role);
    bool IsInRole(string roleName);
}
