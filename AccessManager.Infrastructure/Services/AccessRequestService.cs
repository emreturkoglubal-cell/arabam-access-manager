using AccessManager.Application;
using AccessManager.Application.Interfaces;
using AccessManager.Domain.Constants;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.Infrastructure.Repositories;

namespace AccessManager.Infrastructure.Services;

public class AccessRequestService : IAccessRequestService
{
    private readonly IAccessRequestRepository _requestRepo;
    private readonly IApprovalStepRepository _stepRepo;
    private readonly IPersonnelRepository _personnelRepo;
    private readonly IResourceSystemRepository _systemRepo;
    private readonly IPersonnelAccessService _accessService;
    private readonly IAuditService _auditService;

    public AccessRequestService(
        IAccessRequestRepository requestRepo,
        IApprovalStepRepository stepRepo,
        IPersonnelRepository personnelRepo,
        IResourceSystemRepository systemRepo,
        IPersonnelAccessService accessService,
        IAuditService auditService)
    {
        _requestRepo = requestRepo;
        _stepRepo = stepRepo;
        _personnelRepo = personnelRepo;
        _systemRepo = systemRepo;
        _accessService = accessService;
        _auditService = auditService;
    }

    public IReadOnlyList<AccessRequest> GetAll() => _requestRepo.GetAll();

    public IReadOnlyList<AccessRequest> GetPendingForApprover(int approverId)
    {
        var managedIds = _personnelRepo.GetByManagerId(approverId).Select(p => p.Id).ToHashSet();
        var systemOwnerIds = _systemRepo.GetAll().Where(s => s.OwnerId == approverId).Select(s => s.Id).ToHashSet();
        var all = _requestRepo.GetAll();
        return all
            .Where(r => r.Status == AccessRequestStatus.PendingManager && managedIds.Contains(r.PersonnelId)
                || r.Status == AccessRequestStatus.PendingSystemOwner && systemOwnerIds.Contains(r.ResourceSystemId)
                || r.Status == AccessRequestStatus.PendingIT)
            .ToList();
    }

    public IReadOnlyList<AccessRequest> GetByPersonnelId(int personnelId) => _requestRepo.GetByPersonnelId(personnelId);

    public AccessRequest? GetById(int id) => _requestRepo.GetById(id);

    public IReadOnlyList<ApprovalStep> GetApprovalSteps(int requestId) => _stepRepo.GetByAccessRequestId(requestId);

    public AccessRequest Create(AccessRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.CreatedAt = SystemTime.Now;
        request.Status = AccessRequestStatus.PendingManager;
        request.Id = _requestRepo.Insert(request);
        _stepRepo.Insert(new ApprovalStep
        {
            AccessRequestId = request.Id,
            StepName = ApprovalStepNames.Manager,
            Order = 1
        });
        var person = _personnelRepo.GetById(request.CreatedBy);
        _auditService.Log(AuditAction.RequestCreated, request.CreatedBy, person != null ? person.FirstName + " " + person.LastName : "?", "AccessRequest", request.Id.ToString(), request.Reason);
        return request;
    }

    public void ApproveStep(int requestId, string stepName, int approverId, string? approverDisplayName, bool approved, string? comment = null)
    {
        var step = _stepRepo.GetStep(requestId, stepName);
        if (step == null) return;

        _stepRepo.UpdateApproval(step.Id, approverId, approverDisplayName, SystemTime.Now, approved, comment);

        var request = _requestRepo.GetById(requestId);
        if (request == null) return;

        var actorName = !string.IsNullOrWhiteSpace(approverDisplayName)
            ? approverDisplayName.Trim()
            : (_personnelRepo.GetById(approverId) is { } approver ? $"{approver.FirstName} {approver.LastName}" : "?");

        if (!approved)
        {
            _requestRepo.UpdateStatus(requestId, AccessRequestStatus.Rejected);
            _auditService.Log(AuditAction.RequestRejected, approverId, actorName, "AccessRequest", requestId.ToString(), comment);
            return;
        }

        if (stepName == ApprovalStepNames.Manager)
        {
            var sys = _systemRepo.GetById(request.ResourceSystemId);
            if (sys?.OwnerId != null && sys.OwnerId != approverId)
            {
                _requestRepo.UpdateStatus(requestId, AccessRequestStatus.PendingSystemOwner);
                _stepRepo.Insert(new ApprovalStep { AccessRequestId = requestId, StepName = ApprovalStepNames.SystemOwner, Order = 2 });
            }
            else
            {
                _requestRepo.UpdateStatus(requestId, AccessRequestStatus.PendingIT);
                _stepRepo.Insert(new ApprovalStep { AccessRequestId = requestId, StepName = ApprovalStepNames.IT, Order = 2 });
            }
        }
        else if (stepName == ApprovalStepNames.SystemOwner)
        {
            _requestRepo.UpdateStatus(requestId, AccessRequestStatus.PendingIT);
            var steps = _stepRepo.GetByAccessRequestId(requestId);
            if (!steps.Any(s => s.StepName == ApprovalStepNames.IT))
                _stepRepo.Insert(new ApprovalStep { AccessRequestId = requestId, StepName = ApprovalStepNames.IT, Order = 3 });
        }
        else if (stepName == ApprovalStepNames.IT)
        {
            _requestRepo.UpdateStatus(requestId, AccessRequestStatus.Approved);
            _auditService.Log(AuditAction.RequestApproved, approverId, actorName, "AccessRequest", requestId.ToString(), comment);
        }
    }

    public void MarkAsApplied(int requestId, int? appliedById = null, string? appliedByName = null)
    {
        var request = _requestRepo.GetById(requestId);
        if (request == null || request.Status != AccessRequestStatus.Approved) return;
        _requestRepo.UpdateStatus(requestId, AccessRequestStatus.Applied);
        _accessService.Grant(request.PersonnelId, request.ResourceSystemId, request.RequestedPermission, true, request.EndDate, requestId);
        int? actorId = appliedById;
        string actorName = !string.IsNullOrWhiteSpace(appliedByName)
            ? appliedByName.Trim()
            : (appliedById.HasValue && _personnelRepo.GetById(appliedById.Value) is { } p ? $"{p.FirstName} {p.LastName}" : "?");
        if (!actorId.HasValue)
        {
            var createdByPerson = _personnelRepo.GetById(request.CreatedBy);
            actorId = request.CreatedBy;
            actorName = createdByPerson != null ? $"{createdByPerson.FirstName} {createdByPerson.LastName}" : "?";
        }
        _auditService.Log(AuditAction.RequestApplied, actorId, actorName, "AccessRequest", requestId.ToString());
    }
}
