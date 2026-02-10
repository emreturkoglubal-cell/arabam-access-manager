namespace AccessManager.Domain.Enums;

public enum AccessRequestStatus
{
    Draft,
    PendingManager,
    PendingSystemOwner,
    PendingIT,
    Approved,
    Rejected,
    Applied,
    Expired
}
