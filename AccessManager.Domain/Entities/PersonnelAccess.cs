using AccessManager.Domain.Enums;

namespace AccessManager.Domain.Entities;

public class PersonnelAccess
{
    public Guid Id { get; set; }
    public Guid PersonnelId { get; set; }
    public Guid ResourceSystemId { get; set; }
    public PermissionType PermissionType { get; set; }
    public bool IsException { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public Guid? GrantedByRequestId { get; set; }

    public Personnel? Personnel { get; set; }
    public ResourceSystem? ResourceSystem { get; set; }
}
