using AccessManager.Models;
using AccessManager.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.Pages.AccessRequests;

public class DetailModel : PageModel
{
    private readonly IAccessRequestService _requestService;
    private readonly IPersonnelService _personnelService;
    private readonly ISystemService _systemService;

    public DetailModel(IAccessRequestService requestService, IPersonnelService personnelService, ISystemService systemService)
    {
        _requestService = requestService;
        _personnelService = personnelService;
        _systemService = systemService;
    }

    public AccessRequest? AccessRequestItem { get; set; }
    public IReadOnlyList<ApprovalStep> Steps { get; set; } = new List<ApprovalStep>();
    public string? PersonName { get; set; }
    public string? SystemName { get; set; }
    public Dictionary<Guid, string> ApproverNames { get; set; } = new();

    public bool CanApprove { get; set; }
    public string? NextStepName { get; set; }

    public IActionResult OnGet(Guid id)
    {
        AccessRequestItem = _requestService.GetById(id);
        if (AccessRequestItem == null) return NotFound();
        Steps = _requestService.GetApprovalSteps(id);
        var person = _personnelService.GetById(AccessRequestItem.PersonnelId);
        PersonName = person != null ? $"{person.FirstName} {person.LastName}" : null;
        SystemName = _systemService.GetById(AccessRequestItem.ResourceSystemId)?.Name;
        foreach (var s in Steps.Where(s => s.ApprovedBy.HasValue))
        {
            var a = _personnelService.GetById(s.ApprovedBy!.Value);
            ApproverNames[s.ApprovedBy.Value] = a != null ? $"{a.FirstName} {a.LastName}" : "-";
        }

        var pending = Steps.FirstOrDefault(s => s.Approved == null);
        NextStepName = pending?.StepName;
        CanApprove = AccessRequestItem.Status == AccessRequestStatus.PendingManager || AccessRequestItem.Status == AccessRequestStatus.PendingSystemOwner || AccessRequestItem.Status == AccessRequestStatus.PendingIT;
        return Page();
    }

    public IActionResult OnPostApprove(Guid id, string stepName, bool approved, string? comment)
    {
        var req = _requestService.GetById(id);
        if (req == null) return NotFound();
        // Mock: approverId = ilk aktif personel (yönetici). Gerçekte HttpContext.User'dan alınır.
        var approverId = _personnelService.GetActive().FirstOrDefault()?.Id ?? Guid.Empty;
        _requestService.ApproveStep(id, stepName, approverId, approved, comment);
        req = _requestService.GetById(id);
        if (approved && req?.Status == AccessRequestStatus.Approved)
            _requestService.MarkAsApplied(id);
        return RedirectToPage("Detail", new { id });
    }
}
