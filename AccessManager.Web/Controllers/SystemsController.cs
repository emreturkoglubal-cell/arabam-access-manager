using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using AccessManager.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class SystemsController : Controller
{
    private readonly ISystemService _systemService;
    private readonly IPersonnelService _personnelService;
    private readonly IPersonnelAccessService _accessService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;

    public SystemsController(
        ISystemService systemService,
        IPersonnelService personnelService,
        IPersonnelAccessService accessService,
        IAuditService auditService,
        ICurrentUserService currentUser)
    {
        _systemService = systemService;
        _personnelService = personnelService;
        _accessService = accessService;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var systems = _systemService.GetAll();
        var ownerNames = new Dictionary<int, string>();
        var accessCounts = new Dictionary<int, int>();
        foreach (var s in systems)
        {
            if (s.OwnerId.HasValue)
            {
                var o = _personnelService.GetById(s.OwnerId.Value);
                ownerNames[s.Id] = o != null ? $"{o.FirstName} {o.LastName}" : "-";
            }
            accessCounts[s.Id] = _accessService.GetActive().Count(a => a.ResourceSystemId == s.Id);
        }
        ViewBag.Systems = systems;
        ViewBag.OwnerNames = ownerNames;
        ViewBag.AccessCounts = accessCounts;
        return View();
    }

    [HttpGet]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Create()
    {
        ViewBag.PersonnelList = _personnelService.GetActive();
        return View(new SystemEditInputModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Create(SystemEditInputModel input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            ModelState.AddModelError(nameof(input.Name), "Sistem adı gerekli.");
            ViewBag.PersonnelList = _personnelService.GetActive();
            return View(input);
        }
        var system = new ResourceSystem
        {
            Name = input.Name.Trim(),
            Code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim(),
            SystemType = input.SystemType,
            CriticalLevel = input.CriticalLevel,
            OwnerId = input.OwnerId == 0 ? null : input.OwnerId,
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim()
        };
        _systemService.Create(system);
        var actorName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
        _auditService.Log(AuditAction.SystemCreated, _currentUser.UserId, actorName, "ResourceSystem", system.Id.ToString(), $"Sistem: {system.Name}");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Edit(int id)
    {
        var system = _systemService.GetById(id);
        if (system == null) return NotFound();
        ViewBag.System = system;
        ViewBag.PersonnelList = _personnelService.GetActive();
        return View(new SystemEditInputModel
        {
            Name = system.Name,
            Code = system.Code,
            SystemType = system.SystemType,
            CriticalLevel = system.CriticalLevel,
            OwnerId = system.OwnerId ?? 0,
            Description = system.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Edit(int id, SystemEditInputModel input)
    {
        var system = _systemService.GetById(id);
        if (system == null) return NotFound();
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            ModelState.AddModelError(nameof(input.Name), "Sistem adı gerekli.");
            ViewBag.System = system;
            ViewBag.PersonnelList = _personnelService.GetActive();
            return View(input);
        }
        system.Name = input.Name.Trim();
        system.Code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim();
        system.SystemType = input.SystemType;
        system.CriticalLevel = input.CriticalLevel;
        system.OwnerId = input.OwnerId == 0 ? null : input.OwnerId;
        system.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        _systemService.Update(system);
        var actorName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
        _auditService.Log(AuditAction.SystemUpdated, _currentUser.UserId, actorName, "ResourceSystem", system.Id.ToString(), $"Sistem: {system.Name}");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Delete(int id)
    {
        var system = _systemService.GetById(id);
        if (system == null) return NotFound();
        ViewBag.System = system;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Delete")]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult DeletePost(int id)
    {
        var system = _systemService.GetById(id);
        if (system == null) return NotFound();
        var deleted = _systemService.Delete(id);
        if (!deleted)
        {
            ViewBag.System = system;
            ViewBag.DeleteFailed = true;
            ViewBag.DeleteFailedMessage = "Bu sistem yetki talebi, rol yetkisi veya personel erişiminde kullanıldığı için silinemiyor.";
            return View("Delete");
        }
        var actorName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
        _auditService.Log(AuditAction.SystemDeleted, _currentUser.UserId, actorName, "ResourceSystem", id.ToString(), $"Silinen sistem: {system.Name}");
        return RedirectToAction(nameof(Index));
    }
}
