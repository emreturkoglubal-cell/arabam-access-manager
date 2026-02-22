using AccessManager.Domain.Enums;

namespace AccessManager.Domain.Entities;

/// <summary>
/// Erişim talebi. Bir personelin belirli bir kaynak sistemde (ResourceSystem) yetki (PermissionType) istemesi.
/// Onay süreci: Yönetici → Sistem sahibi → IT. Onaylanırsa PersonnelAccess oluşturulur; reddedilirse talep Rejected kalır.
/// </summary>
public class AccessRequest
{
    public int Id { get; set; }
    public int PersonnelId { get; set; }
    public int ResourceSystemId { get; set; }
    public PermissionType RequestedPermission { get; set; }
    public string? Reason { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public AccessRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }

    public Personnel? Personnel { get; set; }
    public ResourceSystem? ResourceSystem { get; set; }
}
