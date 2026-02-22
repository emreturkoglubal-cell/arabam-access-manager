namespace AccessManager.Domain.Enums;

/// <summary>
/// Kaynak sistemin kritiklik seviyesi (düşük / orta / yüksek). Onay süreçleri veya raporlamada kullanılabilir.
/// </summary>
public enum CriticalLevel
{
    /// <summary>Düşük kritiklik.</summary>
    Low,
    /// <summary>Orta kritiklik.</summary>
    Medium,
    /// <summary>Yüksek kritiklik.</summary>
    High
}
