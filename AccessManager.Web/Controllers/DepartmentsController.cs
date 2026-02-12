using AccessManager.Application.Interfaces;
using AccessManager.UI.Constants;
using AccessManager.UI.ViewModels;
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
        var countByDept = _personnelService.GetPersonnelCountByDepartment();

        ViewBag.Departments = departments;
        ViewBag.PersonnelCountByDepartment = countByDept;
        return View();
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new DepartmentCreateInputModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(DepartmentCreateInputModel input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            ModelState.AddModelError(nameof(input.Name), "Departman adı gerekli.");
            return View(input);
        }
        _departmentService.Add(input.Name, input.Code, input.Description);
        return RedirectToAction(nameof(Index));
    }
}
