using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PersonnelEntity = AccessManager.Domain.Entities.Personnel;

namespace AccessManager.UI.Pages.Personnel;

[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class IndexModel : PageModel
{
    private readonly IPersonnelService _personnelService;
    private readonly IDepartmentService _departmentService;
    private readonly IRoleService _roleService;

    public IndexModel(IPersonnelService personnelService, IDepartmentService departmentService, IRoleService roleService)
    {
        _personnelService = personnelService;
        _departmentService = departmentService;
        _roleService = roleService;
    }

    public IReadOnlyList<PersonnelEntity> PersonnelList { get; set; } = new List<PersonnelEntity>();
    public IReadOnlyList<Department> Departments { get; set; } = new List<Department>();
    public IReadOnlyList<Role> Roles { get; set; } = new List<Role>();
    public Dictionary<Guid, string> ManagerNames { get; set; } = new();
    public Guid? FilterDepartmentId { get; set; }
    public bool? FilterActiveOnly { get; set; } = true;

    public void OnGet(Guid? departmentId, bool? activeOnly)
    {
        FilterDepartmentId = departmentId;
        FilterActiveOnly = activeOnly ?? true;
        Departments = _departmentService.GetAll();
        Roles = _roleService.GetAll();

        var list = FilterActiveOnly == true ? _personnelService.GetActive() : _personnelService.GetAll();
        if (FilterDepartmentId.HasValue)
            list = list.Where(p => p.DepartmentId == FilterDepartmentId.Value).ToList();
        PersonnelList = list;

        foreach (var mId in list.Where(p => p.ManagerId.HasValue).Select(p => p.ManagerId!.Value).Distinct())
        {
            var m = _personnelService.GetById(mId);
            if (m != null) ManagerNames[mId] = $"{m.FirstName} {m.LastName}";
        }
    }
}
