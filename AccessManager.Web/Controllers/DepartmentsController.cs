using AccessManager.Application.Interfaces;
using AccessManager.UI.Constants;
using AccessManager.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

/// <summary>
/// Departman yönetimi: liste (personel sayısı ile), yeni departman oluşturma. Personeller bir departmana bağlıdır.
/// Yetki: Admin veya Manager.
/// </summary>
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

    /// <summary>GET /Departments/Index — Tüm departmanları ve her birindeki personel sayısını listeler.</summary>
    [HttpGet]
    public IActionResult Index()
    {
        var departments = _departmentService.GetAll();
        var countByDept = _personnelService.GetPersonnelCountByDepartment();

        ViewBag.Departments = departments;
        ViewBag.PersonnelCountByDepartment = countByDept;
        return View();
    }

    /// <summary>GET /Departments/Create — Yeni departman oluşturma formu.</summary>
    [HttpGet]
    public IActionResult Create()
    {
        return View(new DepartmentCreateInputModel());
    }

    /// <summary>POST /Departments/Create — Yeni departman kaydı oluşturur.</summary>
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
