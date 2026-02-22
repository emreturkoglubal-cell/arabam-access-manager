using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.Application.Interfaces;

/// <summary>
/// Personel erişimleri (PersonnelAccess): personelin hangi sistemde hangi yetki (PermissionType) ile erişimi olduğu; Grant/Revoke/Reactivate.
/// İstisna (isException) ve süre sınırı (expiresAt), talep kaydı (requestId) ile ilişkilendirilebilir.
/// </summary>
public interface IPersonnelAccessService
{
    /// <summary>Belirtilen personelin tüm erişim kayıtlarını döner.</summary>
    IReadOnlyList<PersonnelAccess> GetByPersonnel(int personnelId);
    /// <summary>Aktif (revoke edilmemiş, süresi dolmamış) tüm erişimleri döner.</summary>
    IReadOnlyList<PersonnelAccess> GetActive();
    /// <summary>Belirtilen gün sayısı içinde süresi dolacak erişimler (uyarı/rapor için).</summary>
    IReadOnlyList<PersonnelAccess> GetExpiringWithinDays(int days);
    /// <summary>İstisna olarak işaretlenmiş erişimler (Exception raporu için).</summary>
    IReadOnlyList<PersonnelAccess> GetExceptions();
    /// <summary>Personel için belirtilen sistemde yetki açar; isException, expiresAt ve requestId isteğe bağlı.</summary>
    void Grant(int personnelId, int resourceSystemId, PermissionType permissionType, bool isException, DateTime? expiresAt = null, int? requestId = null);
    /// <summary>Erişimi iptal eder (RevokedAt kaydedilir, artık aktif sayılmaz).</summary>
    void Revoke(int personnelAccessId);
    /// <summary>Daha önce iptal edilmiş erişimi tekrar aktif eder.</summary>
    void Reactivate(int personnelAccessId);
}
