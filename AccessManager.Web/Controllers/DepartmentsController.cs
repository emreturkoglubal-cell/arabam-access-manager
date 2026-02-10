using AccessManager.Application.Interfaces;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class DepartmentsController : Controller
{
    private readonly IDepartmentService _departmentService;
    private readonly IPersonnelService _personnelService;

    public DepartmentsController(IDepartmentService departmentService, IPersonnelService personnelService)
    {
        _departmentService = departmentService;
        _personnelService = personnelService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var departments = _departmentService.GetAll();
        var countByDept = new Dictionary<int, int>();
        foreach (var d in departments)
            countByDept[d.Id] = _personnelService.GetByDepartmentId(d.Id).Count;

        ViewBag.Departments = departments;
        ViewBag.PersonnelCountByDepartment = countByDept;
        return View();
    }
}
