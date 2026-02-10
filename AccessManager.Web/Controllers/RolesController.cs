using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class RolesController : Controller
{
    private readonly IRoleService _roleService;
    private readonly ISystemService _systemService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;

    public RolesController(IRoleService roleService, ISystemService systemService, IAuditService auditService, ICurrentUserService currentUser)
    {
        _roleService = roleService;
        _systemService = systemService;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var roles = _roleService.GetAll();
        var systems = _systemService.GetAll().ToDictionary(s => s.Id, s => s.Name ?? s.Code ?? s.Id.ToString());
        var details = new Dictionary<Guid, List<(string SystemName, string Permission)>>();
        foreach (var role in roles)
        {
            var perms = _roleService.GetPermissionsByRole(role.Id);
            details[role.Id] = perms.Select(p => (systems.GetValueOrDefault(p.ResourceSystemId, p.ResourceSystemId.ToString()), Helpers.StatusLabels.PermissionTypeLabel(p.PermissionType))).ToList();
        }
        ViewBag.Roles = roles;
        ViewBag.RolePermissionDetails = details;
        return View();
    }

    [HttpGet]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Create() => View(new RoleEditInputModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Create(RoleEditInputModel input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            ModelState.AddModelError(nameof(input.Name), "Rol adı gerekli.");
            return View(input);
        }
        var role = new Role
        {
            Name = input.Name.Trim(),
            Code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim(),
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim()
        };
        _roleService.CreateRole(role);
        var actorName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
        _auditService.Log(AuditAction.RoleCreated, _currentUser.UserId, actorName, "Role", role.Id.ToString(), $"Rol: {role.Name}");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Edit(Guid id)
    {
        var role = _roleService.GetById(id);
        if (role == null) return NotFound();
        ViewBag.Role = role;
        return View(new RoleEditInputModel { Name = role.Name, Code = role.Code, Description = role.Description });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Edit(Guid id, RoleEditInputModel input)
    {
        var role = _roleService.GetById(id);
        if (role == null) return NotFound();
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            ModelState.AddModelError(nameof(input.Name), "Rol adı gerekli.");
            ViewBag.Role = role;
            return View(input);
        }
        role.Name = input.Name.Trim();
        role.Code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim();
        role.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        _roleService.UpdateRole(role);
        var actorName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
        _auditService.Log(AuditAction.RoleUpdated, _currentUser.UserId, actorName, "Role", role.Id.ToString(), $"Rol: {role.Name}");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult EditPermissions(Guid id)
    {
        var role = _roleService.GetById(id);
        if (role == null) return NotFound();
        var systems = _systemService.GetAll();
        ViewBag.Role = role;
        ViewBag.Permissions = _roleService.GetPermissionsByRole(id);
        ViewBag.Systems = systems;
        ViewBag.SystemNames = systems.ToDictionary(s => s.Id, s => s.Name ?? s.Code ?? s.Id.ToString());
        return View(new RoleAddPermissionInputModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult AddPermission(Guid id, RoleAddPermissionInputModel input)
    {
        var role = _roleService.GetById(id);
        if (role == null) return NotFound();
        var rp = _roleService.AddPermissionToRole(id, input.ResourceSystemId, input.PermissionType, input.IsDefault);
        var actorName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
        _auditService.Log(AuditAction.RolePermissionAdded, _currentUser.UserId, actorName, "RolePermission", rp.Id.ToString(), $"Rol: {role.Name}");
        return RedirectToAction(nameof(EditPermissions), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult RemovePermission(Guid id, Guid permissionId)
    {
        var role = _roleService.GetById(id);
        if (role == null) return NotFound();
        if (_roleService.RemoveRolePermission(permissionId))
        {
            var actorName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
            _auditService.Log(AuditAction.RolePermissionRemoved, _currentUser.UserId, actorName, "RolePermission", permissionId.ToString(), $"Rol: {role.Name}");
        }
        return RedirectToAction(nameof(EditPermissions), new { id });
    }

    [HttpGet]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Delete(Guid id)
    {
        var role = _roleService.GetById(id);
        if (role == null) return NotFound();
        ViewBag.Role = role;
        ViewBag.DeleteFailed = false;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Delete")]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult DeletePost(Guid id)
    {
        var role = _roleService.GetById(id);
        if (role == null) return NotFound();
        var deleted = _roleService.DeleteRole(id);
        if (!deleted)
        {
            ViewBag.Role = role;
            ViewBag.DeleteFailed = true;
            ViewBag.DeleteFailedMessage = "Bu rol en az bir personelde atanmış olduğu için silinemiyor.";
            return View("Delete");
        }
        var actorName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
        _auditService.Log(AuditAction.RoleDeleted, _currentUser.UserId, actorName, "Role", id.ToString(), $"Silinen rol: {role.Name}");
        return RedirectToAction(nameof(Index));
    }
}

public class RoleEditInputModel
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
}

public class RoleAddPermissionInputModel
{
    public Guid ResourceSystemId { get; set; }
    public PermissionType PermissionType { get; set; }
    public bool IsDefault { get; set; } = true;
}
