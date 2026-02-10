using AccessManager.Models;
using AccessManager.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.Pages.Roles;

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
    public Dictionary<Guid, List<(string SystemName, string Permission)>> RolePermissionDetails { get; set; } = new();

    public void OnGet()
    {
        Roles = _roleService.GetAll();
        var systems = _systemService.GetAll().ToDictionary(s => s.Id, s => s.Name);
        foreach (var role in Roles)
        {
            var perms = _roleService.GetPermissionsByRole(role.Id);
            RolePermissionDetails[role.Id] = perms
                .Select(p => (systems.GetValueOrDefault(p.ResourceSystemId, p.ResourceSystemId.ToString()), p.PermissionType.ToString()))
                .ToList();
        }
    }
}
