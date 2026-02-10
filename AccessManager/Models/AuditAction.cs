namespace AccessManager.Models;

public enum AuditAction
{
    PersonnelCreated,
    PersonnelUpdated,
    PersonnelOffboarded,
    AccessGranted,
    AccessRevoked,
    RequestCreated,
    RequestApproved,
    RequestRejected,
    RequestApplied,
    RoleAssigned,
    SystemCreated,
    SystemUpdated
}
