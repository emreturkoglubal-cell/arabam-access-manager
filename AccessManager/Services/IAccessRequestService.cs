using AccessManager.Models;

namespace AccessManager.Services;

public interface IAccessRequestService
{
    IReadOnlyList<AccessRequest> GetAll();
    IReadOnlyList<AccessRequest> GetPendingForApprover(Guid approverId);
    IReadOnlyList<AccessRequest> GetByPersonnelId(Guid personnelId);
    AccessRequest? GetById(Guid id);
    IReadOnlyList<ApprovalStep> GetApprovalSteps(Guid requestId);
    AccessRequest Create(AccessRequest request);
    void ApproveStep(Guid requestId, string stepName, Guid approverId, bool approved, string? comment = null);
    void MarkAsApplied(Guid requestId);
}
