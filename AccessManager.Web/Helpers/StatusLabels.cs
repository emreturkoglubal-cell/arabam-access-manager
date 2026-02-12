using AccessManager.Domain.Enums;

namespace AccessManager.UI.Helpers;

// PermissionType için Türkçe: Faz 1'de sadece Açık/Kapalı kullanılıyor

/// <summary>
/// Enum ve sabit değerlerin Türkçe UI metinleri.
/// </summary>
public static class StatusLabels
{
    public static string AccessRequestStatus(AccessRequestStatus status)
    {
        return status switch
        {
            AccessManager.Domain.Enums.AccessRequestStatus.Draft => "Taslak",
            AccessManager.Domain.Enums.AccessRequestStatus.PendingManager => "Yönetici onayı bekliyor",
            AccessManager.Domain.Enums.AccessRequestStatus.PendingSystemOwner => "Sistem sahibi onayı bekliyor",
            AccessManager.Domain.Enums.AccessRequestStatus.PendingIT => "IT onayı bekliyor",
            AccessManager.Domain.Enums.AccessRequestStatus.Approved => "Onaylandı",
            AccessManager.Domain.Enums.AccessRequestStatus.Rejected => "Reddedildi",
            AccessManager.Domain.Enums.AccessRequestStatus.Applied => "Uygulandı",
            AccessManager.Domain.Enums.AccessRequestStatus.Expired => "Süresi doldu",
            _ => status.ToString()
        };
    }

    public static string PersonnelStatus(PersonnelStatus status)
    {
        return status switch
        {
            AccessManager.Domain.Enums.PersonnelStatus.Active => "Aktif",
            AccessManager.Domain.Enums.PersonnelStatus.Passive => "Pasif",
            AccessManager.Domain.Enums.PersonnelStatus.Offboarded => "İşten ayrıldı",
            _ => status.ToString()
        };
    }

    public static string AuditActionLabel(AuditAction action)
    {
        return action switch
        {
            AuditAction.Login => "Giriş",
            AuditAction.Logout => "Çıkış",
            AuditAction.PersonnelCreated => "Personel oluşturuldu",
            AuditAction.PersonnelUpdated => "Personel güncellendi",
            AuditAction.PersonnelNoteAdded => "Personel notu eklendi",
            AuditAction.PersonnelOffboarded => "Personel işten çıkarıldı",
            AuditAction.AccessGranted => "Yetki verildi",
            AuditAction.AccessRevoked => "Yetki kaldırıldı",
            AuditAction.RequestCreated => "Talep oluşturuldu",
            AuditAction.RequestApproved => "Talep onaylandı",
            AuditAction.RequestRejected => "Talep reddedildi",
            AuditAction.RequestApplied => "Talep uygulandı",
            AuditAction.RoleAssigned => "Rol atandı",
            AuditAction.RoleCreated => "Rol oluşturuldu",
            AuditAction.RoleUpdated => "Rol güncellendi",
            AuditAction.RoleDeleted => "Rol silindi",
            AuditAction.RolePermissionAdded => "Rol yetkisi eklendi",
            AuditAction.RolePermissionRemoved => "Rol yetkisi kaldırıldı",
            AuditAction.SystemCreated => "Sistem oluşturuldu",
            AuditAction.SystemUpdated => "Sistem güncellendi",
            AuditAction.SystemDeleted => "Sistem silindi",
            AuditAction.AssetCreated => "Donanım oluşturuldu",
            AuditAction.AssetUpdated => "Donanım güncellendi",
            AuditAction.AssetDeleted => "Donanım silindi",
            AuditAction.AssetAssigned => "Donanım zimmetlendi",
            AuditAction.AssetReturned => "Donanım iade edildi",
            AuditAction.AssetAssignmentNoteAdded => "Zimmet notu eklendi",
            AuditAction.ReviseRequestCreated => "Geliştirici talebi oluşturuldu",
            AuditAction.ReviseRequestStatusUpdated => "Geliştirici talebi durumu güncellendi",
            AuditAction.Other => "Diğer",
            _ => action.ToString()
        };
    }

    /// <summary>Denetim kaydı hedef türü (string) Türkçe karşılığı.</summary>
    public static string AuditTargetType(string? targetType)
    {
        if (string.IsNullOrEmpty(targetType)) return "—";
        return targetType switch
        {
            "Personnel" => "Personel",
            "Access" => "Erişim",
            "AccessRequest" => "Yetki talebi",
            "Role" => "Rol",
            "System" => "Sistem",
            "Asset" => "Donanım",
            "AssetAssignment" => "Zimmet",
            _ => targetType
        };
    }

    public static string AssetTypeLabel(AssetType type)
    {
        return type switch
        {
            AssetType.Laptop => "Dizüstü bilgisayar",
            AssetType.Desktop => "Masaüstü bilgisayar",
            AssetType.Monitor => "Monitör",
            AssetType.Phone => "Telefon",
            AssetType.Tablet => "Tablet",
            AssetType.Keyboard => "Klavye",
            AssetType.Mouse => "Fare",
            AssetType.Other => "Diğer",
            _ => type.ToString()
        };
    }

    /// <summary>Faz 1: Yetkilerde Read/Write yok, sadece Açık/Kapalı.</summary>
    public static string PermissionTypeLabel(PermissionType type)
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

    public static string AssetStatusLabel(AssetStatus status)
    {
        return status switch
        {
            AssetStatus.Available => "Müsait",
            AssetStatus.Assigned => "Zimmette",
            AssetStatus.InRepair => "Bakımda",
            AssetStatus.Retired => "Hurdaya çıkarıldı",
            _ => status.ToString()
        };
    }

    public static string ReviseRequestStatusLabel(ReviseRequestStatus status)
    {
        return status switch
        {
            ReviseRequestStatus.Pending => "Çözülmedi",
            ReviseRequestStatus.Resolved => "Çözüldü",
            _ => status.ToString()
        };
    }
}
