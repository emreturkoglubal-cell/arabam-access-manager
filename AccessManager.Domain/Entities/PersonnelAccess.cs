using AccessManager.Domain.Enums;

namespace AccessManager.Domain.Entities;

/// <summary>
/// Personelin bir kaynak sistemdeki fiili erişimi. Yetki türü (PermissionType), rol dışı mı (IsException), verilme ve bitiş tarihi.
/// Erişim talebi onaylanıp uygulandığında veya manuel GrantAccess ile oluşturulur; RevokeAccess ile kaldırılır.
/// </summary>
public class PersonnelAccess
{
    public int Id { get; set; }
    public int PersonnelId { get; set; }
    public int ResourceSystemId { get; set; }
    public PermissionType PermissionType { get; set; }
    public bool IsException { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public int? GrantedByRequestId { get; set; }

    public Personnel? Personnel { get; set; }
    public ResourceSystem? ResourceSystem { get; set; }
}
