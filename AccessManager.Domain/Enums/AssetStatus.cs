namespace AccessManager.Domain.Enums;

/// <summary>Donanım durumu: müsait, zimmette, bakımda, hurdaya çıkarıldı.</summary>
public enum AssetStatus
{
    Available,
    Assigned,
    InRepair,
    Retired
}
