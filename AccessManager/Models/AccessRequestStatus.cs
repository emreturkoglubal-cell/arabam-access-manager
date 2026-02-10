namespace AccessManager.Models;

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
