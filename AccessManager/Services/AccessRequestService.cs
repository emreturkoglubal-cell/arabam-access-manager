using AccessManager.Data;
using AccessManager.Models;

namespace AccessManager.Services;

public class AccessRequestService : IAccessRequestService
{
    private readonly MockDataStore _store = MockDataStore.Current;
    private readonly IPersonnelAccessService _accessService;
    private readonly IAuditService _auditService;

    public AccessRequestService(IPersonnelAccessService accessService, IAuditService auditService)
    {
        _accessService = accessService;
        _auditService = auditService;
    }

    public IReadOnlyList<AccessRequest> GetAll() => _store.AccessRequests.ToList();

    public IReadOnlyList<AccessRequest> GetPendingForApprover(Guid approverId)
    {
        var personnel = _store.Personnel.FirstOrDefault(p => p.Id == approverId);
        if (personnel == null) return new List<AccessRequest>();
        var managedIds = _store.Personnel.Where(p => p.ManagerId == approverId).Select(p => p.Id).ToHashSet();
        var systemOwnerIds = _store.ResourceSystems.Where(s => s.OwnerId == approverId).Select(s => s.Id).ToHashSet();
        return _store.AccessRequests
            .Where(r => r.Status == AccessRequestStatus.PendingManager && managedIds.Contains(r.PersonnelId)
                        || r.Status == AccessRequestStatus.PendingSystemOwner && systemOwnerIds.Contains(r.ResourceSystemId)
                        || r.Status == AccessRequestStatus.PendingIT)
            .ToList();
    }

    public IReadOnlyList<AccessRequest> GetByPersonnelId(Guid personnelId) =>
        _store.AccessRequests.Where(r => r.PersonnelId == personnelId).ToList();

    public AccessRequest? GetById(Guid id) => _store.AccessRequests.FirstOrDefault(r => r.Id == id);

    public IReadOnlyList<ApprovalStep> GetApprovalSteps(Guid requestId) =>
        _store.ApprovalSteps.Where(s => s.AccessRequestId == requestId).OrderBy(s => s.Order).ToList();

    public AccessRequest Create(AccessRequest request)
    {
        request.Id = Guid.NewGuid();
        request.CreatedAt = DateTime.UtcNow;
        request.Status = AccessRequestStatus.PendingManager;
        _store.AccessRequests.Add(request);
        _store.ApprovalSteps.Add(new ApprovalStep
        {
            Id = Guid.NewGuid(),
            AccessRequestId = request.Id,
            StepName = "Manager",
            Order = 1
        });
        var person = _store.Personnel.FirstOrDefault(p => p.Id == request.CreatedBy);
        _auditService.Log(AuditAction.RequestCreated, request.CreatedBy, person?.FirstName + " " + person?.LastName ?? "?", "AccessRequest", request.Id.ToString(), request.Reason);
        return request;
    }

    public void ApproveStep(Guid requestId, string stepName, Guid approverId, bool approved, string? comment = null)
    {
        var step = _store.ApprovalSteps.FirstOrDefault(s => s.AccessRequestId == requestId && s.StepName == stepName);
        if (step == null) return;
        step.ApprovedBy = approverId;
        step.ApprovedAt = DateTime.UtcNow;
        step.Approved = approved;
        step.Comment = comment;

        var request = GetById(requestId);
        if (request == null) return;

        var approver = _store.Personnel.FirstOrDefault(p => p.Id == approverId);
        var actorName = approver != null ? $"{approver.FirstName} {approver.LastName}" : "?";

        if (!approved)
        {
            request.Status = AccessRequestStatus.Rejected;
            _auditService.Log(AuditAction.RequestRejected, approverId, actorName, "AccessRequest", requestId.ToString(), comment);
            return;
        }

        if (stepName == "Manager")
        {
            var sys = _store.ResourceSystems.FirstOrDefault(s => s.Id == request.ResourceSystemId);
            if (sys?.OwnerId != null && sys.OwnerId != approverId)
            {
                request.Status = AccessRequestStatus.PendingSystemOwner;
                var nextStep = new ApprovalStep { Id = Guid.NewGuid(), AccessRequestId = requestId, StepName = "SystemOwner", Order = 2 };
                _store.ApprovalSteps.Add(nextStep);
            }
            else
            {
                request.Status = AccessRequestStatus.PendingIT;
                var nextStep = new ApprovalStep { Id = Guid.NewGuid(), AccessRequestId = requestId, StepName = "IT", Order = 2 };
                _store.ApprovalSteps.Add(nextStep);
            }
        }
        else if (stepName == "SystemOwner")
        {
            request.Status = AccessRequestStatus.PendingIT;
            if (!_store.ApprovalSteps.Any(s => s.AccessRequestId == requestId && s.StepName == "IT"))
                _store.ApprovalSteps.Add(new ApprovalStep { Id = Guid.NewGuid(), AccessRequestId = requestId, StepName = "IT", Order = 3 });
        }
        else if (stepName == "IT")
        {
            request.Status = AccessRequestStatus.Approved;
            _auditService.Log(AuditAction.RequestApproved, approverId, actorName, "AccessRequest", requestId.ToString(), comment);
        }
    }

    public void MarkAsApplied(Guid requestId)
    {
        var request = GetById(requestId);
        if (request == null || request.Status != AccessRequestStatus.Approved) return;
        request.Status = AccessRequestStatus.Applied;
        _accessService.Grant(request.PersonnelId, request.ResourceSystemId, request.RequestedPermission, true, request.EndDate, requestId);
        var approver = _store.Personnel.FirstOrDefault(p => p.Id == request.CreatedBy);
        _auditService.Log(AuditAction.RequestApplied, request.CreatedBy, approver?.FirstName + " " + approver?.LastName ?? "?", "AccessRequest", requestId.ToString());
    }
}
