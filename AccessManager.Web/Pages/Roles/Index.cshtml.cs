using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.UI.Constants;
using AccessManager.UI.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.UI.Pages.Roles;

[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class IndexModel : PageModel
{
    private readonly IRoleService _roleService;
    private readonly ISystemService _systemService;

    public IndexModel(IRoleService roleService, ISystemService systemService)
    {
        _roleService = roleService;
        _systemService = systemService;
    }

    public IReadOnlyList<Role> Roles { get; set; } = new List<Role>();
    public Dictionary<int, List<(string SystemName, string Permission)>> RolePermissionDetails { get; set; } = new();

    public void OnGet()
    {
        Roles = _roleService.GetAll();
        var systems = _systemService.GetAll().ToDictionary(s => s.Id, s => s.Name);
        foreach (var role in Roles)
        {
            var perms = _roleService.GetPermissionsByRole(role.Id);
            RolePermissionDetails[role.Id] = perms
                .Select(p => (systems.GetValueOrDefault(p.ResourceSystemId, p.ResourceSystemId.ToString()), StatusLabels.PermissionTypeLabel(p.PermissionType)))
                .ToList();
        }
    }
}
