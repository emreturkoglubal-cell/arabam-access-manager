using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.UI.Pages.AccessRequests;

[Authorize(Roles = AuthorizationRolePolicies.AdminManagerUser)]
public class IndexModel : PageModel
{
    private readonly IAccessRequestService _requestService;
    private readonly IPersonnelService _personnelService;
    private readonly ISystemService _systemService;

    public IndexModel(IAccessRequestService requestService, IPersonnelService personnelService, ISystemService systemService)
    {
        _requestService = requestService;
        _personnelService = personnelService;
        _systemService = systemService;
    }

    public IReadOnlyList<AccessRequest> Requests { get; set; } = new List<AccessRequest>();
    public Dictionary<int, string> PersonNames { get; set; } = new();
    public Dictionary<int, string> SystemNames { get; set; } = new();
    public int? FilterPersonnelId { get; set; }
    public string? FilterStatus { get; set; }

    public void OnGet(int? personnelId, string? status)
    {
        FilterPersonnelId = personnelId;
        FilterStatus = status;
        var list = _requestService.GetAll();
        if (personnelId.HasValue) list = list.Where(r => r.PersonnelId == personnelId.Value).ToList();
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<AccessRequestStatus>(status, out var s))
            list = list.Where(r => r.Status == s).ToList();
        Requests = list;

        foreach (var pid in Requests.Select(r => r.PersonnelId).Distinct())
        {
            var p = _personnelService.GetById(pid);
            PersonNames[pid] = p != null ? $"{p.FirstName} {p.LastName}" : pid.ToString();
        }
        foreach (var sid in Requests.Select(r => r.ResourceSystemId).Distinct())
        {
            var sys = _systemService.GetById(sid);
            SystemNames[sid] = sys?.Name ?? sid.ToString();
        }
    }
}
