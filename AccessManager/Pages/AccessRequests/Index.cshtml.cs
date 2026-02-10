using AccessManager.Models;
using AccessManager.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.Pages.AccessRequests;

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
    public Dictionary<Guid, string> PersonNames { get; set; } = new();
    public Dictionary<Guid, string> SystemNames { get; set; } = new();
    public Guid? FilterPersonnelId { get; set; }
    public string? FilterStatus { get; set; }

    public void OnGet(Guid? personnelId, string? status)
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
