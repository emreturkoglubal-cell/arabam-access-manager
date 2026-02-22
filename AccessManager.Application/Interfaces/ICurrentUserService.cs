using AccessManager.Domain.Enums;

namespace AccessManager.Application.Interfaces;

/// <summary>
/// Oturum açmış kullanıcı bilgisi: kimlik, görünen ad, uygulama rolü (AppRole); yetki kontrolü için IsInRole.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Kullanıcı giriş yapmış mı.</summary>
    bool IsAuthenticated { get; }
    /// <summary>Kullanıcı ID (Personnel/AppUser ile eşleşir).</summary>
    int? UserId { get; }
    /// <summary>Giriş adı (kullanıcı adı).</summary>
    string? UserName { get; }
    /// <summary>Görünen ad (örn. Ad Soyad).</summary>
    string? DisplayName { get; }
    /// <summary>Uygulama rolü: Admin, Manager, Auditor, User.</summary>
    AppRole? Role { get; }
    /// <summary>Belirtilen rol kontrolü.</summary>
    bool IsInRole(AppRole role);
    /// <summary>Rol adı ile kontrol (örn. "Admin").</summary>
    bool IsInRole(string roleName);
}
