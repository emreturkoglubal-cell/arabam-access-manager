namespace AccessManager.Domain.Enums;

/// <summary>
/// Erişim talebinin (AccessRequest) yaşam döngüsü durumu.
/// Talep oluşturulur (Draft), sonra sırayla Yönetici / Sistem Sahibi / IT onayı bekler, onaylanır/reddedilir, uygulanır veya süresi dolar.
/// </summary>
public enum AccessRequestStatus
{
    /// <summary>Taslak; henüz gönderilmedi.</summary>
    Draft,
    /// <summary>Personelin yöneticisi onayı bekleniyor.</summary>
    PendingManager,
    /// <summary>Sistem sahibi onayı bekleniyor.</summary>
    PendingSystemOwner,
    /// <summary>IT onayı bekleniyor.</summary>
    PendingIT,
    /// <summary>Tüm onaylar tamamlandı; henüz erişim uygulanmadı.</summary>
    Approved,
    /// <summary>Talep reddedildi.</summary>
    Rejected,
    /// <summary>Erişim personel kaydına uygulandı (PersonnelAccess oluşturuldu).</summary>
    Applied,
    /// <summary>Talebin veya erişimin süresi doldu.</summary>
    Expired
}
