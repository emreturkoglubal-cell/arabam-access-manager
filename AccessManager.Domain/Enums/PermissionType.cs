namespace AccessManager.Domain.Enums;

public enum PermissionType
{
    Read,
    Write,
    Admin,
    Custom,
    /// <summary>Faz 1: Açık yetki (tek kutucuk).</summary>
    Open,
    /// <summary>Faz 1: Kapalı yetki (tek kutucuk).</summary>
    Closed
}
