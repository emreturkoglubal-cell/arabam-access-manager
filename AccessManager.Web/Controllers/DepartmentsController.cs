using System.Text.Json;
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
    private readonly IRoleService _roleService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;
    private readonly IReportService _reportService;

    public DepartmentsController(
        IDepartmentService departmentService,
        IPersonnelService personnelService,
        IRoleService roleService,
        IAuditService auditService,
        ICurrentUserService currentUser,
        IReportService reportService)
    {
        _departmentService = departmentService;
        _personnelService = personnelService;
        _roleService = roleService;
        _auditService = auditService;
        _currentUser = currentUser;
        _reportService = reportService;
    }

    /// <summary>GET /Departments/Index — Tüm departmanları ve her birindeki personel sayısını listeler (hiyerarşi satırları).</summary>
    [HttpGet]
    public IActionResult Index()
    {
        var departments = _departmentService.GetAll().ToList();
        var countByDept = _personnelService.GetPersonnelCountByDepartment();
        var topManagerIds = departments.Where(d => d.TopManagerPersonnelId.HasValue).Select(d => d.TopManagerPersonnelId!.Value).Distinct().ToList();
        var topManagerPersonnel = topManagerIds.Count > 0 ? _personnelService.GetByIds(topManagerIds) : new List<Personnel>();
        var topManagerNames = topManagerPersonnel.ToDictionary(p => p.Id, p => $"{p.FirstName} {p.LastName}");
        ViewBag.Departments = departments;
        ViewBag.PersonnelCountByDepartment = countByDept;
        ViewBag.TopManagerNames = topManagerNames;

        var byId = departments.ToDictionary(d => d.Id);
        var roots = departments.Where(d => !d.ParentId.HasValue).OrderBy(d => d.Name).ToList();
        var rows = new List<(Department Dept, int Level)>();
        void Visit(Department d, int level)
        {
            rows.Add((d, level));
            foreach (var c in departments.Where(x => x.ParentId == d.Id).OrderBy(x => x.Name))
                Visit(c, level + 1);
        }
        foreach (var r in roots)
            Visit(r, 0);
        foreach (var orphan in departments.Where(d => d.ParentId.HasValue && !byId.ContainsKey(d.ParentId.Value)).OrderBy(d => d.Name))
        {
            if (!rows.Any(x => x.Dept.Id == orphan.Id))
                Visit(orphan, 0);
        }
        ViewBag.DepartmentRows = rows;

        return View();
    }

    /// <summary>GET /Departments/Detail/{id} — Departman detayı (bilgiler + bu departmandaki aktif personel listesi, sayfalı).</summary>
    [HttpGet]
    public IActionResult Detail(int id, int page = 1)
    {
        var department = _departmentService.GetById(id);
        if (department == null) return NotFound();
        var countByDept = _personnelService.GetPersonnelCountByDepartment();
        var personnelCount = countByDept.TryGetValue(id, out var c) ? c : 0;
        ViewBag.Department = department;
        ViewBag.PersonnelCount = personnelCount;

        var pageSize = 10;
        if (page < 1) page = 1;
        var paged = _personnelService.GetPaged(departmentId: id, roleId: null, statusFilter: "active", search: null, page, pageSize);
        ViewBag.PersonnelList = paged.Items;
        ViewBag.PersonnelTotalCount = paged.TotalCount;
        ViewBag.PersonnelPageNumber = paged.PageNumber;
        ViewBag.PersonnelTotalPages = (paged.TotalCount + pageSize - 1) / pageSize;
        ViewBag.PersonnelPageSize = pageSize;

        var roles = _roleService.GetAll();
        ViewBag.RoleNames = roles.ToDictionary(r => r.Id, r => r.Name ?? "—");
        ViewBag.DepartmentNames = new Dictionary<int, string> { { department.Id, department.Name ?? "—" } };
        var managerIds = paged.Items.Where(p => p.ManagerId.HasValue).Select(p => p.ManagerId!.Value).Distinct().ToList();
        var managers = managerIds.Count > 0 ? _personnelService.GetByIds(managerIds) : new List<Personnel>();
        ViewBag.ManagerNames = managers.ToDictionary(m => m.Id, m => $"{m.FirstName} {m.LastName}");
        var deptManagers = _departmentService.GetDepartmentManagers(id);
        var deptManagerPersonnelIds = deptManagers.Select(m => m.PersonnelId).Distinct().ToList();
        var deptManagerPersonnel = deptManagerPersonnelIds.Count > 0 ? _personnelService.GetByIds(deptManagerPersonnelIds) : new List<Personnel>();
        ViewBag.DepartmentManagers = deptManagers;
        ViewBag.DepartmentManagerPersonnel = deptManagerPersonnel.ToDictionary(p => p.Id, p => p);
        ViewBag.AllPersonnelForGmy = _personnelService.GetActive();

        var turnover = _reportService.GetDepartmentTurnoverPoints(id, 12);
        ViewBag.DepartmentTurnoverJson = turnover.Count > 0
            ? JsonSerializer.Serialize(new
            {
                labels = turnover.Select(t => t.Label).ToList(),
                hires = turnover.Select(t => t.Hires).ToList(),
                exits = turnover.Select(t => t.Exits).ToList()
            })
            : null;
        ViewBag.DepartmentLicenseUsd = _reportService.GetDepartmentActiveLicenseCostUsd(id);

        var allDepts = _departmentService.GetAll().Where(d => d.Id != id).OrderBy(d => d.Name).ToList();
        ViewBag.ParentDepartmentChoices = allDepts;

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

        var all = _departmentService.GetAll().ToList();
        if (WouldCreateParentCycle(all, id, input.ParentId))
        {
            TempData["DepartmentEditError"] = "Üst departman seçimi döngü oluşturur (kendi altına bağlanamaz).";
            return RedirectToAction(nameof(Detail), new { id });
        }

        department.Name = input.Name.Trim();
        department.Code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim();
        department.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        department.TopManagerPersonnelId = input.TopManagerPersonnelId;
        department.ParentId = input.ParentId;
        _departmentService.Update(department);
        _auditService.Log(AuditAction.Other, _currentUser.UserId, _currentUser.DisplayName ?? _currentUser.UserName ?? "?", "Department", id.ToString(), $"Departman güncellendi: {department.Name}");
        TempData["DepartmentEditSuccess"] = "Departman bilgileri güncellendi.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    /// <summary>GET /Departments/Create — Yeni departman oluşturma formu.</summary>
    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.AllDepartments = _departmentService.GetAll().OrderBy(d => d.Name).ToList();
        ViewBag.AllPersonnelForGmy = _personnelService.GetActive();
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
            ViewBag.AllDepartments = _departmentService.GetAll().OrderBy(d => d.Name).ToList();
            ViewBag.AllPersonnelForGmy = _personnelService.GetActive();
            return View(input);
        }
        _departmentService.Add(input.Name, input.Code, input.Description, input.ParentId, input.TopManagerPersonnelId);
        return RedirectToAction(nameof(Index));
    }

    private static bool WouldCreateParentCycle(IReadOnlyList<Department> all, int departmentId, int? newParentId)
    {
        if (!newParentId.HasValue) return false;
        if (newParentId.Value == departmentId) return true;
        var byId = all.ToDictionary(d => d.Id);
        var walk = newParentId;
        var guard = 0;
        while (walk.HasValue && byId.TryGetValue(walk.Value, out var node) && guard++ < 200)
        {
            if (walk.Value == departmentId) return true;
            walk = node.ParentId;
        }
        return false;
    }
}
