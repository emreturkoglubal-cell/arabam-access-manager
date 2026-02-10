using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.UI.Pages.Systems;

[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class IndexModel : PageModel
{
    private readonly ISystemService _systemService;
    private readonly IPersonnelService _personnelService;
    private readonly IPersonnelAccessService _accessService;

    public IndexModel(ISystemService systemService, IPersonnelService personnelService, IPersonnelAccessService accessService)
    {
        _systemService = systemService;
        _personnelService = personnelService;
        _accessService = accessService;
    }

    public IReadOnlyList<ResourceSystem> Systems { get; set; } = new List<ResourceSystem>();
    public Dictionary<Guid, string> OwnerNames { get; set; } = new();
    public Dictionary<Guid, int> AccessCounts { get; set; } = new();

    public void OnGet()
    {
        Systems = _systemService.GetAll();
        foreach (var s in Systems)
        {
            if (s.OwnerId.HasValue)
            {
                var o = _personnelService.GetById(s.OwnerId.Value);
                OwnerNames[s.Id] = o != null ? $"{o.FirstName} {o.LastName}" : "-";
            }
            AccessCounts[s.Id] = _accessService.GetActive().Count(a => a.ResourceSystemId == s.Id);
        }
    }
}
