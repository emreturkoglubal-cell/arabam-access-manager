using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using AccessManager.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

/// <summary>
/// Departman yönetimi: liste (personel sayısı ile), detay, yeni departman oluşturma, güncelleme. Personeller bir departmana bağlıdır.
/// Yetki: Admin veya Manager.
/// </summary>
[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class DepartmentsController : Controller
{
    private readonly IDepartmentService _departmentService;
    private readonly IPersonnelService _personnelService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;

    public DepartmentsController(IDepartmentService departmentService, IPersonnelService personnelService, IAuditService auditService, ICurrentUserService currentUser)
    {
        _departmentService = departmentService;
        _personnelService = personnelService;
        _auditService = auditService;
        _currentUser = currentUser;
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

    /// <summary>GET /Departments/Detail/{id} — Departman detayı (bilgiler + personel sayısı).</summary>
    [HttpGet]
    public IActionResult Detail(int id)
    {
        var department = _departmentService.GetById(id);
        if (department == null) return NotFound();
        var countByDept = _personnelService.GetPersonnelCountByDepartment();
        var personnelCount = countByDept.TryGetValue(id, out var c) ? c : 0;
        ViewBag.Department = department;
        ViewBag.PersonnelCount = personnelCount;
        return View();
    }

    /// <summary>POST /Departments/UpdateDepartment/{id} — Departman bilgilerini günceller.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateDepartment(int id, DepartmentEditInputModel input)
    {
        var department = _departmentService.GetById(id);
        if (department == null) return NotFound();
        if (input == null || string.IsNullOrWhiteSpace(input.Name))
        {
            TempData["DepartmentEditError"] = "Departman adı zorunludur.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        department.Name = input.Name.Trim();
        department.Code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim();
        department.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        _departmentService.Update(department);
        _auditService.Log(AuditAction.Other, _currentUser.UserId, _currentUser.DisplayName ?? _currentUser.UserName ?? "?", "Department", id.ToString(), $"Departman güncellendi: {department.Name}");
        TempData["DepartmentEditSuccess"] = "Departman bilgileri güncellendi.";
        return RedirectToAction(nameof(Detail), new { id });
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
