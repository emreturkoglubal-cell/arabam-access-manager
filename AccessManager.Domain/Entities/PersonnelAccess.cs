using AccessManager.Domain.Enums;

namespace AccessManager.Domain.Entities;

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
