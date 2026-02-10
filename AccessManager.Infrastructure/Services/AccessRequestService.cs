using AccessManager.Application.Interfaces;
using AccessManager.Domain.Constants;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.Infrastructure.Data;

namespace AccessManager.Infrastructure.Services;

public class AccessRequestService : IAccessRequestService
{
    private readonly MockDataStore _store;
    private readonly IPersonnelAccessService _accessService;
    private readonly IAuditService _auditService;

    public AccessRequestService(MockDataStore store, IPersonnelAccessService accessService, IAuditService auditService)
    {
        _store = store;
        _accessService = accessService;
        _auditService = auditService;
    }

    public IReadOnlyList<AccessRequest> GetAll() => _store.AccessRequests.ToList();

    public IReadOnlyList<AccessRequest> GetPendingForApprover(Guid approverId)
    {
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
        ArgumentNullException.ThrowIfNull(request);
        request.Id = Guid.NewGuid();
        request.CreatedAt = DateTime.UtcNow;
        request.Status = AccessRequestStatus.PendingManager;
        _store.AccessRequests.Add(request);
        _store.ApprovalSteps.Add(new ApprovalStep
        {
            Id = Guid.NewGuid(),
            AccessRequestId = request.Id,
            StepName = ApprovalStepNames.Manager,
            Order = 1
        });
        var person = _store.Personnel.FirstOrDefault(p => p.Id == request.CreatedBy);
        _auditService.Log(AuditAction.RequestCreated, request.CreatedBy, person != null ? person.FirstName + " " + person.LastName : "?", "AccessRequest", request.Id.ToString(), request.Reason);
        return request;
    }

    public void ApproveStep(Guid requestId, string stepName, Guid approverId, string? approverDisplayName, bool approved, string? comment = null)
    {
        var step = _store.ApprovalSteps.FirstOrDefault(s => s.AccessRequestId == requestId && s.StepName == stepName);
        if (step == null) return;
        step.ApprovedBy = approverId;
        step.ApprovedByName = approverDisplayName;
        step.ApprovedAt = DateTime.UtcNow;
        step.Approved = approved;
        step.Comment = comment;

        var request = GetById(requestId);
        if (request == null) return;

        var actorName = !string.IsNullOrWhiteSpace(approverDisplayName)
            ? approverDisplayName.Trim()
            : (_store.Personnel.FirstOrDefault(p => p.Id == approverId) is { } approver ? $"{approver.FirstName} {approver.LastName}" : "?");

        if (!approved)
        {
            request.Status = AccessRequestStatus.Rejected;
            _auditService.Log(AuditAction.RequestRejected, approverId, actorName, "AccessRequest", requestId.ToString(), comment);
            return;
        }

        if (stepName == ApprovalStepNames.Manager)
        {
            var sys = _store.ResourceSystems.FirstOrDefault(s => s.Id == request.ResourceSystemId);
            if (sys?.OwnerId != null && sys.OwnerId != approverId)
            {
                request.Status = AccessRequestStatus.PendingSystemOwner;
                _store.ApprovalSteps.Add(new ApprovalStep { Id = Guid.NewGuid(), AccessRequestId = requestId, StepName = ApprovalStepNames.SystemOwner, Order = 2 });
            }
            else
            {
                request.Status = AccessRequestStatus.PendingIT;
                _store.ApprovalSteps.Add(new ApprovalStep { Id = Guid.NewGuid(), AccessRequestId = requestId, StepName = ApprovalStepNames.IT, Order = 2 });
            }
        }
        else if (stepName == ApprovalStepNames.SystemOwner)
        {
            request.Status = AccessRequestStatus.PendingIT;
            if (!_store.ApprovalSteps.Any(s => s.AccessRequestId == requestId && s.StepName == ApprovalStepNames.IT))
                _store.ApprovalSteps.Add(new ApprovalStep { Id = Guid.NewGuid(), AccessRequestId = requestId, StepName = ApprovalStepNames.IT, Order = 3 });
        }
        else if (stepName == ApprovalStepNames.IT)
        {
            request.Status = AccessRequestStatus.Approved;
            _auditService.Log(AuditAction.RequestApproved, approverId, actorName, "AccessRequest", requestId.ToString(), comment);
        }
    }

    public void MarkAsApplied(Guid requestId, Guid? appliedById = null, string? appliedByName = null)
    {
        var request = GetById(requestId);
        if (request == null || request.Status != AccessRequestStatus.Approved) return;
        request.Status = AccessRequestStatus.Applied;
        _accessService.Grant(request.PersonnelId, request.ResourceSystemId, request.RequestedPermission, true, request.EndDate, requestId);
        Guid? actorId = appliedById;
        string actorName = !string.IsNullOrWhiteSpace(appliedByName)
            ? appliedByName.Trim()
            : (appliedById.HasValue && _store.Personnel.FirstOrDefault(p => p.Id == appliedById.Value) is { } p ? $"{p.FirstName} {p.LastName}" : "?");
        if (!actorId.HasValue)
        {
            var createdByPerson = _store.Personnel.FirstOrDefault(p => p.Id == request.CreatedBy);
            actorId = request.CreatedBy;
            actorName = createdByPerson != null ? $"{createdByPerson.FirstName} {createdByPerson.LastName}" : "?";
        }
        _auditService.Log(AuditAction.RequestApplied, actorId, actorName, "AccessRequest", requestId.ToString());
    }
}
