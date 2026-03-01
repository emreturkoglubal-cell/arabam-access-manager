using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
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

    public OnboardingController(
        IPersonnelService personnelService,
        IManagerService managerService,
        IDepartmentService departmentService,
        IRoleService roleService,
        IPersonnelAccessService accessService,
        IAuditService auditService,
        ITeamService teamService)
    {
        _personnelService = personnelService;
        _managerService = managerService;
        _departmentService = departmentService;
        _roleService = roleService;
        _accessService = accessService;
        _auditService = auditService;
        _teamService = teamService;
    }

    /// <summary>GET /Onboarding/Index — İşe giriş formu (departman, ekip, rol, yönetici listeleri).</summary>
    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.Departments = _departmentService.GetAll();
        ViewBag.Teams = _teamService.GetAll();
        ViewBag.Roles = _roleService.GetAll();
        ViewBag.Managers = _managerService.GetActiveManagerPersonnel();
        var oneMonthAgo = DateTime.Today.AddMonths(-1);
        ViewBag.RecentHires = _personnelService.GetActive().Where(p => p.StartDate >= oneMonthAgo).OrderByDescending(p => p.StartDate).Take(100).ToList();
        return View(new PersonnelCreateInputModel());
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
