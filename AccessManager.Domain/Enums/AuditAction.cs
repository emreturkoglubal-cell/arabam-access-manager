namespace AccessManager.Domain.Enums;

public enum AuditAction
{
    Login,
    Logout,
    PersonnelCreated,
    PersonnelUpdated,
    PersonnelNoteAdded,
    PersonnelOffboarded,
    AccessGranted,
    AccessRevoked,
    RequestCreated,
    RequestApproved,
    RequestRejected,
    RequestApplied,
    RoleAssigned,
    RoleCreated,
    RoleUpdated,
    RoleDeleted,
    RolePermissionAdded,
    RolePermissionRemoved,
    SystemCreated,
    SystemUpdated,
    SystemDeleted,
    AssetCreated,
    AssetUpdated,
    AssetDeleted,
    AssetAssigned,
    AssetReturned,
    AssetAssignmentNoteAdded
}
