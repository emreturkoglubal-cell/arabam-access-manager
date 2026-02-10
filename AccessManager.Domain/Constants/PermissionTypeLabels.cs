using AccessManager.Domain.Enums;

namespace AccessManager.Domain.Constants;

/// <summary>Yetkilerde Read/Write yok, sadece Açık/Kapalı (Faz 1).</summary>
public static class PermissionTypeLabels
{
    public static string Get(PermissionType type)
    {
        return type switch
        {
            PermissionType.Open => "Açık",
            PermissionType.Closed => "Kapalı",
            PermissionType.Read => "Açık",
            PermissionType.Write => "Kapalı",
            PermissionType.Admin => "Açık",
            PermissionType.Custom => "Açık",
            _ => type.ToString()
        };
    }
}
