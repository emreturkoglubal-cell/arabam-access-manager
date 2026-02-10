namespace AccessManager.Models;

public class AccessRequest
{
    public Guid Id { get; set; }
    public Guid PersonnelId { get; set; }
    public Guid ResourceSystemId { get; set; }
    public PermissionType RequestedPermission { get; set; }
    public string? Reason { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public AccessRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }

    public Personnel? Personnel { get; set; }
    public ResourceSystem? ResourceSystem { get; set; }
}
