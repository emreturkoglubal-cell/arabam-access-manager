using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

[Authorize(Roles = AuthorizationRolePolicies.AdminManagerUser)]
public class AccessRequestsController : Controller
{
    private readonly IAccessRequestService _requestService;
    private readonly IPersonnelService _personnelService;
    private readonly ISystemService _systemService;
    private readonly ICurrentUserService _currentUser;

    public AccessRequestsController(
        IAccessRequestService requestService,
        IPersonnelService personnelService,
        ISystemService systemService,
        ICurrentUserService currentUser)
    {
        _requestService = requestService;
        _personnelService = personnelService;
        _systemService = systemService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public IActionResult Index(int? personnelId, string? status)
    {
        var list = _requestService.GetAll();
        if (personnelId.HasValue) list = list.Where(r => r.PersonnelId == personnelId.Value).ToList();
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<AccessRequestStatus>(status, out var s))
            list = list.Where(r => r.Status == s).ToList();

        var personnelIds = list.Select(r => r.PersonnelId).Distinct().ToList();
        var systemIds = list.Select(r => r.ResourceSystemId).Distinct().ToList();
        var personnelList = personnelIds.Count > 0 ? _personnelService.GetByIds(personnelIds) : new List<Personnel>();
        var systemsList = systemIds.Count > 0 ? _systemService.GetByIds(systemIds) : new List<ResourceSystem>();
        var personNames = personnelList.ToDictionary(p => p.Id, p => $"{p.FirstName} {p.LastName}");
        var systemNames = systemsList.ToDictionary(s => s.Id, s => s.Name ?? s.Code ?? s.Id.ToString());

        ViewBag.Requests = list;
        ViewBag.PersonNames = personNames;
        ViewBag.SystemNames = systemNames;
        ViewBag.FilterPersonnelId = personnelId;
        ViewBag.FilterStatus = status;
        return View();
    }

    [HttpGet]
    public IActionResult Create(int? personnelId)
    {
        ViewBag.PersonnelList = _personnelService.GetActive();
        ViewBag.Systems = _systemService.GetAll();
        Personnel? preselectedPerson = null;
        if (personnelId.HasValue && personnelId.Value != 0)
        {
            preselectedPerson = _personnelService.GetById(personnelId.Value);
            if (preselectedPerson != null)
                ViewBag.PreselectedPerson = preselectedPerson;
        }
        return View(new AccessRequestCreateInputModel { PersonnelId = personnelId ?? 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(AccessRequestCreateInputModel input)
    {
        if (input.PersonnelId == 0 || input.ResourceSystemId == 0)
        {
            ModelState.AddModelError(string.Empty, "Personel ve sistem seçiniz.");
            ViewBag.PersonnelList = _personnelService.GetActive();
            ViewBag.Systems = _systemService.GetAll();
            return View(input);
        }
        var person = _personnelService.GetById(input.PersonnelId);
        if (person == null) return NotFound();
        var request = new AccessRequest
        {
            PersonnelId = input.PersonnelId,
            ResourceSystemId = input.ResourceSystemId,
            RequestedPermission = input.RequestedPermission,
            Reason = input.Reason,
            EndDate = input.EndDate,
            CreatedBy = input.PersonnelId
        };
        _requestService.Create(request);
        return RedirectToAction(nameof(Detail), new { id = request.Id });
    }

    [HttpGet]
    public IActionResult Detail(int id)
    {
        var req = _requestService.GetById(id);
        if (req == null) return NotFound();
        var steps = _requestService.GetApprovalSteps(id);
        var person = _personnelService.GetById(req.PersonnelId);
        var approverNames = new Dictionary<int, string>();
        foreach (var s in steps.Where(s => s.ApprovedBy.HasValue))
        {
            approverNames[s.ApprovedBy!.Value] = !string.IsNullOrWhiteSpace(s.ApprovedByName)
                ? s.ApprovedByName
                : _personnelService.GetById(s.ApprovedBy.Value) is { } a ? $"{a.FirstName} {a.LastName}" : "-";
        }
        var pending = steps.FirstOrDefault(s => s.Approved == null);
        var canApprove = req.Status == AccessRequestStatus.PendingManager || req.Status == AccessRequestStatus.PendingSystemOwner || req.Status == AccessRequestStatus.PendingIT;

        ViewBag.AccessRequestItem = req;
        ViewBag.Steps = steps;
        ViewBag.PersonName = person != null ? $"{person.FirstName} {person.LastName}" : null;
        ViewBag.SystemName = _systemService.GetById(req.ResourceSystemId)?.Name;
        ViewBag.ApproverNames = approverNames;
        ViewBag.NextStepName = pending?.StepName;
        ViewBag.CanApprove = canApprove;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Approve(int id, string stepName, bool approved, string? comment)
    {
        var req = _requestService.GetById(id);
        if (req == null) return NotFound();
        var approverId = _currentUser.UserId ?? 0;
        var approverDisplayName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
        _requestService.ApproveStep(id, stepName, approverId, approverDisplayName, approved, comment);
        req = _requestService.GetById(id);
        if (approved && req?.Status == AccessRequestStatus.Approved)
            _requestService.MarkAsApplied(id, _currentUser.UserId, _currentUser.DisplayName ?? _currentUser.UserName);
        return RedirectToAction(nameof(Detail), new { id });
    }
}

public class AccessRequestCreateInputModel
{
    public int PersonnelId { get; set; }
    public int ResourceSystemId { get; set; }
    public PermissionType RequestedPermission { get; set; }
    public string? Reason { get; set; }
    public DateTime? EndDate { get; set; }
}
