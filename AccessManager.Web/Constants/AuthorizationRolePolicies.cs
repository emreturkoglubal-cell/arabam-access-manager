using AccessManager.Domain.Constants;

namespace AccessManager.UI.Constants;

/// <summary>
/// Sayfa yetkilendirmesinde kullanılan rol grupları; [Authorize(Roles = ...)] ile kullanılır.
/// </summary>
public static class AuthorizationRolePolicies
{
    /// <summary>Personel, Departman, Sistem, Rol, İşe Giriş, İşten Çıkış</summary>
    public const string AdminAndManager = AppRoleNames.Admin + "," + AppRoleNames.Manager;

    /// <summary>Yetki Talepleri</summary>
    public const string AdminManagerUser = AppRoleNames.Admin + "," + AppRoleNames.Manager + "," + AppRoleNames.User;

    /// <summary>Raporlar</summary>
    public const string AdminManagerAuditor = AppRoleNames.Admin + "," + AppRoleNames.Manager + "," + AppRoleNames.Auditor;

    /// <summary>Denetim Kaydı</summary>
    public const string AdminAndAuditor = AppRoleNames.Admin + "," + AppRoleNames.Auditor;

    /// <summary>Yalnızca Admin (rol/yetki oluşturma, düzenleme, silme)</summary>
    public const string AdminOnly = AppRoleNames.Admin;
}
