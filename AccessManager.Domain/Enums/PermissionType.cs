namespace AccessManager.Domain.Enums;

/// <summary>
/// Sistem/servis üzerindeki yetki türü. Erişim talebi ve personel erişiminde kullanılır.
/// Read: Sadece okuma. Write: Okuma ve yazma. Admin: Tam yetki. Custom: Özel tanımlı.
/// Open/Closed: Faz 1 basit model (açık/kapalı tek yetki).
/// </summary>
public enum PermissionType
{
    /// <summary>Sadece okuma yetkisi.</summary>
    Read,
    /// <summary>Okuma ve yazma yetkisi.</summary>
    Write,
    /// <summary>Tam yetki (yönetici).</summary>
    Admin,
    /// <summary>Özel tanımlı yetki.</summary>
    Custom,
    /// <summary>Faz 1: Açık yetki (tek kutucuk).</summary>
    Open,
    /// <summary>Faz 1: Kapalı yetki (tek kutucuk).</summary>
    Closed
}
