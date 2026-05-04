using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.Infrastructure.Repositories;
using AccessManager.UI.Constants;
using AccessManager.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

/// <summary>
/// İşe giriş (onboarding): yeni personel ekleme formu ve kayıt. Personel oluşturulduktan sonra atanan role göre (RoleId) rol yetkileri (RolePermission) otomatik PersonnelAccess olarak verilir.
/// Yetki: Admin veya Manager.
/// </summary>
[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class OnboardingController : Controller
{
    private readonly IPersonnelService _personnelService;
    private readonly IManagerService _managerService;
    private readonly IDepartmentService _departmentService;
    private readonly IRoleService _roleService;
    private readonly IPersonnelAccessService _accessService;
    private readonly IAuditService _auditService;
    private readonly ITeamService _teamService;
    private readonly IPositionTitleTemplateRepository _titleTemplateRepo;

    public OnboardingController(
        IPersonnelService personnelService,
        IManagerService managerService,
        IDepartmentService departmentService,
        IRoleService roleService,
        IPersonnelAccessService accessService,
        IAuditService auditService,
        ITeamService teamService,
        IPositionTitleTemplateRepository titleTemplateRepo)
    {
        _personnelService = personnelService;
        _managerService = managerService;
        _departmentService = departmentService;
        _roleService = roleService;
        _accessService = accessService;
        _auditService = auditService;
        _teamService = teamService;
        _titleTemplateRepo = titleTemplateRepo;
    }

    /// <summary>GET /Onboarding/Index — İşe giriş formu; alt listede tarih ve departman filtresi.</summary>
    [HttpGet]
    public IActionResult Index(DateTime? from, DateTime? to, int? departmentId)
    {
        ViewBag.Departments = _departmentService.GetAll();
        ViewBag.Teams = _teamService.GetAll();
        ViewBag.Roles = _roleService.GetAll();
        ViewBag.Managers = _managerService.GetActiveManagerPersonnel();

        var toDate = (to ?? DateTime.Today).Date;
        var fromDate = (from ?? toDate.AddMonths(-1)).Date;
        if (fromDate > toDate)
            (fromDate, toDate) = (toDate, fromDate);

        ViewBag.FilterFrom = fromDate;
        ViewBag.FilterTo = toDate;
        ViewBag.FilterDepartmentId = departmentId;

        var hires = _personnelService.GetByStartDateInRange(fromDate, toDate, departmentId);
        ViewBag.RecentHires = hires;

        return View(new PersonnelCreateInputModel());
    }

    /// <summary>GET /Onboarding/SuggestTitle — departman, ekip, seviyeye göre önerilen ünvan (JSON).</summary>
    [HttpGet]
    public IActionResult SuggestTitle(int? departmentId, int? teamId, string? seniorityLevel)
    {
        var title = _titleTemplateRepo.ResolveTitle(departmentId, teamId, seniorityLevel);
        return Json(new { title });
    }

    /// <summary>POST /Onboarding/Index — Yeni personel kaydı oluşturur; rol atanmışsa o rolün sistem yetkilerini otomatik açar; personel detay sayfasına yönlendirir.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(PersonnelCreateInputModel input)
    {
        if (string.IsNullOrWhiteSpace(input.FirstName) || string.IsNullOrWhiteSpace(input.LastName) || string.IsNullOrWhiteSpace(input.Email))
        {
            ModelState.AddModelError(string.Empty, "Ad, soyad ve e-posta zorunludur.");
            ViewBag.Departments = _departmentService.GetAll();
            ViewBag.Teams = _teamService.GetAll();
            ViewBag.Roles = _roleService.GetAll();
            ViewBag.Managers = _managerService.GetActiveManagerPersonnel();
            var fd = DateTime.Today.AddMonths(-1);
            ViewBag.FilterFrom = fd;
            ViewBag.FilterTo = DateTime.Today;
            ViewBag.FilterDepartmentId = (int?)null;
            ViewBag.RecentHires = _personnelService.GetByStartDateInRange(fd, DateTime.Today, null);
            return View(input);
        }

        var p = new Personnel
        {
            FirstName = input.FirstName.Trim(),
            LastName = input.LastName.Trim(),
            Email = input.Email.Trim(),
            DepartmentId = input.DepartmentId,
            TeamId = input.TeamId,
            Position = input.Position?.Trim(),
            SeniorityLevel = string.IsNullOrWhiteSpace(input.SeniorityLevel) ? null : input.SeniorityLevel.Trim(),
            ManagerId = input.ManagerId,
            StartDate = input.StartDate,
            RoleId = input.RoleId,
            Status = PersonnelStatus.Active
        };
        _personnelService.Add(p);
        if (input.IsManager)
            _managerService.SetPersonAsManager(p.Id, true, input.ManagerId);

        if (input.RoleId.HasValue)
        {
            var rolePerms = _roleService.GetPermissionsByRole(input.RoleId.Value);
            foreach (var rp in rolePerms)
                _accessService.Grant(p.Id, rp.ResourceSystemId, rp.PermissionType, false, null, null);
        }

        _auditService.Log(AuditAction.PersonnelCreated, null, "Sistem", "Personnel", p.Id.ToString(), $"İşe giriş: {p.FirstName} {p.LastName}");
        return RedirectToAction(nameof(PersonnelController.Detail), "Personnel", new { id = p.Id });
    }
}
