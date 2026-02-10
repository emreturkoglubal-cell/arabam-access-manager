using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PersonnelEntity = AccessManager.Domain.Entities.Personnel;

namespace AccessManager.UI.Pages.Personnel;

[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class DetailModel : PageModel
{
    private readonly IPersonnelService _personnelService;
    private readonly IDepartmentService _departmentService;
    private readonly IRoleService _roleService;
    private readonly ISystemService _systemService;
    private readonly IPersonnelAccessService _personnelAccessService;

    public DetailModel(IPersonnelService personnelService, IDepartmentService departmentService,
        IRoleService roleService, ISystemService systemService, IPersonnelAccessService personnelAccessService)
    {
        _personnelService = personnelService;
        _departmentService = departmentService;
        _roleService = roleService;
        _systemService = systemService;
        _personnelAccessService = personnelAccessService;
    }

    public PersonnelEntity? Personnel { get; set; }
    public List<PersonnelAccess> AccessList { get; set; } = new();
    public string? DepartmentName { get; set; }
    public string? RoleName { get; set; }
    public string? ManagerName { get; set; }
    public Dictionary<int, string> SystemNames { get; set; } = new();

    public IActionResult OnGet(int id)
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

    public IActionResult OnPostRevokeAccess(int id, int accessId)
    {
        var personnel = _personnelService.GetById(id);
        if (personnel == null) return NotFound();
        var accesses = _personnelAccessService.GetByPersonnel(id);
        var access = accesses.FirstOrDefault(a => a.Id == accessId);
        if (access == null || !access.IsActive)
        {
            TempData["RevokeError"] = "Yetki bulunamadı veya zaten kapalı.";
            return RedirectToPage(new { id });
        }
        _personnelAccessService.Revoke(accessId);
        TempData["RevokeSuccess"] = "Yetki kapatıldı.";
        return RedirectToPage(new { id });
    }
}
