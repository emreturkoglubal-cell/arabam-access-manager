namespace AccessManager.Domain.Enums;

/// <summary>
/// Denetim (audit) log kayıt türü. Kim, ne zaman, hangi işlemi (giriş, personel oluşturma, erişim verme vb.) yaptı.
/// </summary>
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
    AssetAssignmentNoteAdded,
    ReviseRequestCreated,
    ReviseRequestStatusUpdated,
    Other
}
