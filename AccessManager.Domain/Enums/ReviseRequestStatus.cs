namespace AccessManager.Domain.Enums;

/// <summary>
/// Revizyon talebinin (düzeltme/iyileştirme talebi) durumu. Çözülmedi veya çözüldü.
/// </summary>
public enum ReviseRequestStatus
{
    /// <summary>Beklemede; henüz çözülmedi.</summary>
    Pending = 0,
    /// <summary>Çözüldü.</summary>
    Resolved = 1
}
