using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using AccessManager.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class OnboardingController : Controller
{
    private readonly IPersonnelService _personnelService;
    private readonly IDepartmentService _departmentService;
    private readonly IRoleService _roleService;
    private readonly IPersonnelAccessService _accessService;
    private readonly IAuditService _auditService;

    public OnboardingController(
        IPersonnelService personnelService,
        IDepartmentService departmentService,
        IRoleService roleService,
        IPersonnelAccessService accessService,
        IAuditService auditService)
    {
        _personnelService = personnelService;
        _departmentService = departmentService;
        _roleService = roleService;
        _accessService = accessService;
        _auditService = auditService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.Departments = _departmentService.GetAll();
        ViewBag.Roles = _roleService.GetAll();
        ViewBag.Managers = _personnelService.GetActive();
        return View(new PersonnelCreateInputModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(PersonnelCreateInputModel input)
    {
        if (string.IsNullOrWhiteSpace(input.FirstName) || string.IsNullOrWhiteSpace(input.LastName) || string.IsNullOrWhiteSpace(input.Email))
        {
            ModelState.AddModelError(string.Empty, "Ad, soyad ve e-posta zorunludur.");
            ViewBag.Departments = _departmentService.GetAll();
            ViewBag.Roles = _roleService.GetAll();
            ViewBag.Managers = _personnelService.GetActive();
            return View(input);
        }

        var p = new Personnel
        {
            FirstName = input.FirstName.Trim(),
            LastName = input.LastName.Trim(),
            Email = input.Email.Trim(),
            DepartmentId = input.DepartmentId,
            Position = input.Position?.Trim(),
            ManagerId = input.ManagerId,
            StartDate = input.StartDate,
            RoleId = input.RoleId,
            Status = PersonnelStatus.Active
        };
        _personnelService.Add(p);

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
