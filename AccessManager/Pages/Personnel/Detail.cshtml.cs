using AccessManager.Models;
using AccessManager.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.Pages.Personnel;

public class DetailModel : PageModel
{
    private readonly IPersonnelService _personnelService;
    private readonly IDepartmentService _departmentService;
    private readonly IRoleService _roleService;
    private readonly IPersonnelAccessService _accessService;
    private readonly ISystemService _systemService;

    public DetailModel(IPersonnelService personnelService, IDepartmentService departmentService,
        IRoleService roleService, IPersonnelAccessService accessService, ISystemService systemService)
    {
        _personnelService = personnelService;
        _departmentService = departmentService;
        _roleService = roleService;
        _accessService = accessService;
        _systemService = systemService;
    }

    public Models.Personnel? Personnel { get; set; }
    public List<PersonnelAccess> AccessList { get; set; } = new();
    public string? DepartmentName { get; set; }
    public string? RoleName { get; set; }
    public string? ManagerName { get; set; }
    public Dictionary<Guid, string> SystemNames { get; set; } = new();

    public IActionResult OnGet(Guid id)
    {
        var (personnel, accesses) = _personnelService.GetWithAccesses(id);
        if (personnel == null) return NotFound();
        Personnel = personnel;
        AccessList = accesses;
        DepartmentName = _departmentService.GetById(personnel.DepartmentId)?.Name;
        RoleName = personnel.RoleId.HasValue ? _roleService.GetById(personnel.RoleId.Value)?.Name : null;
        if (personnel.ManagerId.HasValue)
        {
            var m = _personnelService.GetById(personnel.ManagerId.Value);
            ManagerName = m != null ? $"{m.FirstName} {m.LastName}" : null;
        }
        foreach (var sysId in accesses.Select(a => a.ResourceSystemId).Distinct())
        {
            var sys = _systemService.GetById(sysId);
            if (sys != null) SystemNames[sysId] = sys.Name;
        }
        return Page();
    }
}
