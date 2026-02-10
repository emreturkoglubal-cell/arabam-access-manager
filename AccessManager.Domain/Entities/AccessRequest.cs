using AccessManager.Domain.Enums;

namespace AccessManager.Domain.Entities;

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
