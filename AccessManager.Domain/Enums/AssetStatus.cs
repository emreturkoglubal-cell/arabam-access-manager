namespace AccessManager.Domain.Enums;

/// <summary>
/// Donanım (Asset) durumu: müsait, zimmette, bakımda veya hurdaya çıkarıldı.
/// </summary>
public enum AssetStatus
{
    /// <summary>Müsait; zimmete verilebilir.</summary>
    Available,
    /// <summary>Bir personelde zimmette.</summary>
    Assigned,
    /// <summary>Bakımda / onarımda.</summary>
    InRepair,
    /// <summary>Hurdaya çıkarıldı; artık kullanılmıyor.</summary>
    Retired
}
