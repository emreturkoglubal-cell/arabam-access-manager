namespace AccessManager.Domain.Enums;

/// <summary>
/// Donanım (Asset) türü. Laptop, masaüstü, monitör, telefon, tablet, klavye, fare veya diğer.
/// </summary>
public enum AssetType
{
    Laptop,
    Desktop,
    Monitor,
    Phone,
    Tablet,
    Keyboard,
    Mouse,
    /// <summary>Diğer donanım türü.</summary>
    Other
}
