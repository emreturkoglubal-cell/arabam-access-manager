using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.UI.Pages.AccessRequests;

[Authorize(Roles = AuthorizationRolePolicies.AdminManagerUser)]
public class DetailModel : PageModel
{
    private readonly IAccessRequestService _requestService;
    private readonly IPersonnelService _personnelService;
    private readonly ISystemService _systemService;
    private readonly ICurrentUserService _currentUser;

    public DetailModel(IAccessRequestService requestService, IPersonnelService personnelService, ISystemService systemService, ICurrentUserService currentUser)
    {
        _requestService = requestService;
        _personnelService = personnelService;
        _systemService = systemService;
        _currentUser = currentUser;
    }

    public AccessRequest? AccessRequestItem { get; set; }
    public IReadOnlyList<ApprovalStep> Steps { get; set; } = new List<ApprovalStep>();
    public string? PersonName { get; set; }
    public string? SystemName { get; set; }
    public Dictionary<int, string> ApproverNames { get; set; } = new();

    public bool CanApprove { get; set; }
    public string? NextStepName { get; set; }

    public IActionResult OnGet(int id)
    {
        AccessRequestItem = _requestService.GetById(id);
        if (AccessRequestItem == null) return NotFound();
        Steps = _requestService.GetApprovalSteps(id);
        var person = _personnelService.GetById(AccessRequestItem.PersonnelId);
        PersonName = person != null ? $"{person.FirstName} {person.LastName}" : null;
        SystemName = _systemService.GetById(AccessRequestItem.ResourceSystemId)?.Name;
        foreach (var s in Steps.Where(s => s.ApprovedBy.HasValue))
        {
            if (!string.IsNullOrWhiteSpace(s.ApprovedByName))
                ApproverNames[s.ApprovedBy!.Value] = s.ApprovedByName;
            else
            {
                var a = _personnelService.GetById(s.ApprovedBy!.Value);
                ApproverNames[s.ApprovedBy.Value] = a != null ? $"{a.FirstName} {a.LastName}" : "-";
            }
        }

        var pending = Steps.FirstOrDefault(s => s.Approved == null);
        NextStepName = pending?.StepName;
        CanApprove = AccessRequestItem.Status == AccessRequestStatus.PendingManager || AccessRequestItem.Status == AccessRequestStatus.PendingSystemOwner || AccessRequestItem.Status == AccessRequestStatus.PendingIT;
        return Page();
    }

    public IActionResult OnPostApprove(int id, string stepName, bool approved, string? comment)
    {
        var req = _requestService.GetById(id);
        if (req == null) return NotFound();
        var approverId = _currentUser.UserId ?? 0;
        var approverDisplayName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
        _requestService.ApproveStep(id, stepName, approverId, approverDisplayName, approved, comment);
        req = _requestService.GetById(id);
        if (approved && req?.Status == AccessRequestStatus.Approved)
            _requestService.MarkAsApplied(id, _currentUser.UserId, _currentUser.DisplayName ?? _currentUser.UserName);
        return RedirectToPage("Detail", new { id });
    }
}
