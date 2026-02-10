using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.UI.Pages.Departments;

[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class IndexModel : PageModel
{
    private readonly IDepartmentService _departmentService;
    private readonly IPersonnelService _personnelService;

    public IndexModel(IDepartmentService departmentService, IPersonnelService personnelService)
    {
        _departmentService = departmentService;
        _personnelService = personnelService;
    }

    public IReadOnlyList<Department> Departments { get; set; } = new List<Department>();
    public Dictionary<int, int> PersonnelCountByDepartment { get; set; } = new();

    public void OnGet()
    {
        Departments = _departmentService.GetAll();
        foreach (var d in Departments)
            PersonnelCountByDepartment[d.Id] = _personnelService.GetByDepartmentId(d.Id).Count;
    }
}
