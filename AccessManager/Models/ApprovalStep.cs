namespace AccessManager.Models;

public class ApprovalStep
{
    public Guid Id { get; set; }
    public Guid AccessRequestId { get; set; }
    public string StepName { get; set; } = string.Empty; // Manager, SystemOwner, IT
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool? Approved { get; set; } // null = bekliyor, true/false
    public string? Comment { get; set; }
    public int Order { get; set; }

    public AccessRequest? AccessRequest { get; set; }
    public Personnel? Approver { get; set; }
}
