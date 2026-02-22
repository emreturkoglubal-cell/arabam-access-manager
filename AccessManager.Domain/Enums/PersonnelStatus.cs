namespace AccessManager.Domain.Enums;

/// <summary>
/// Personelin iş durumu. Aktif çalışıyor, pasif (izinli vb.) veya işten ayrılmış (offboard).
/// </summary>
public enum PersonnelStatus
{
    /// <summary>Aktif çalışan.</summary>
    Active,
    /// <summary>Pasif (izinli, askıda vb.).</summary>
    Passive,
    /// <summary>İşten ayrıldı (offboard tamamlandı).</summary>
    Offboarded
}
