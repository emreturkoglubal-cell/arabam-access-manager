using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

/// <summary>
/// İş rolleri (Görev / Role): liste, oluşturma, düzenleme, silme ve rol bazlı yetkiler (EditPermissions, AddPermission, RemovePermission).
/// Rolün her kaynak sistem için PermissionType tanımlanır; personel role atandığında bu yetkiler varsayılan olarak uygulanır.
/// Yetki: Liste/EditPermissions için Admin veya Manager; Create/Edit/Delete sadece Admin.
/// </summary>
[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class RolesController : Controller
{
    private readonly IRoleService _roleService;
    private readonly ISystemService _systemService;
    private readonly IPersonnelService _personnelService;
    private readonly IDepartmentService _departmentService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;

    public RolesController(IRoleService roleService, ISystemService systemService, IPersonnelService personnelService, IDepartmentService departmentService, IAuditService auditService, ICurrentUserService currentUser)
    {
        _roleService = roleService;
        _systemService = systemService;
        _personnelService = personnelService;
        _departmentService = departmentService;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    /// <summary>GET /Roles/Detail/{id} — Rol detayı: bilgiler, yetkiler, bu roldeki personel listesi (sayfalı).</summary>
    [HttpGet]
    public IActionResult Detail(int id, int page = 1)
    {
        var role = _roleService.GetById(id);
        if (role == null) return NotFound();

        var permissions = _roleService.GetPermissionsByRole(id);
        var systems = _systemService.GetAll();
        var systemNames = systems.ToDictionary(s => s.Id, s => s.Name ?? s.Code ?? s.Id.ToString());

        var pageSize = 10;
        if (page < 1) page = 1;
        var paged = _personnelService.GetPaged(departmentId: null, roleId: id, statusFilter: null, search: null, page, pageSize);
        var personnelList = paged.Items;
        var personnelTotalCount = paged.TotalCount;
        var personnelTotalPages = (personnelTotalCount + pageSize - 1) / pageSize;

        var departmentIds = personnelList.Select(p => p.DepartmentId).Distinct().ToList();
        var departments = departmentIds.Count > 0 ? _departmentService.GetAll().Where(d => departmentIds.Contains(d.Id)).ToDictionary(d => d.Id, d => d.Name ?? "—") : new Dictionary<int, string>();
        var departmentNames = _departmentService.GetAll().ToDictionary(d => d.Id, d => d.Name ?? "—");
        var managerIds = personnelList.Where(p => p.ManagerId.HasValue).Select(p => p.ManagerId!.Value).Distinct().ToList();
        var managers = managerIds.Count > 0 ? _personnelService.GetByIds(managerIds) : new List<Personnel>();
        var managerNames = managers.ToDictionary(m => m.Id, m => $"{m.FirstName} {m.LastName}");
        var roles = _roleService.GetAll();
        var roleNames = roles.ToDictionary(r => r.Id, r => r.Name ?? "—");

        ViewBag.Role = role;
        ViewBag.Permissions = permissions;
        ViewBag.SystemNames = systemNames;
        ViewBag.PersonnelList = personnelList;
        ViewBag.PersonnelTotalCount = personnelTotalCount;
        ViewBag.PersonnelPageNumber = page;
        ViewBag.PersonnelTotalPages = personnelTotalPages;
        ViewBag.PersonnelPageSize = pageSize;
        ViewBag.DepartmentNames = departmentNames;
        ViewBag.ManagerNames = managerNames;
        ViewBag.RoleNames = roleNames;
        return View();
    }

    /// <summary>GET /Roles/Index — Tüm rolleri listeler (tablo; tıklanınca detay).</summary>
    [HttpGet]
    public IActionResult Index()
    {
        var roles = _roleService.GetAll();
        var personnelCountByRole = _personnelService.GetPersonnelCountByRole();
        ViewBag.Roles = roles;
        ViewBag.PersonnelCountByRole = personnelCountByRole;
        return View();
    }

    /// <summary>GET /Roles/Create — Yeni rol oluşturma formu (Admin).</summary>
    [HttpGet]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Create() => View(new RoleEditInputModel());

    /// <summary>POST /Roles/Create — Yeni iş rolü kaydı oluşturur (Admin).</summary>
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
        var created = _roleService.CreateRole(role);
        var actorName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
        _auditService.Log(AuditAction.RoleCreated, _currentUser.UserId, actorName, "Role", created.Id.ToString(), $"Rol: {created.Name}");
        TempData["RoleEditSuccess"] = "Rol oluşturuldu.";
        return RedirectToAction(nameof(Detail), new { id = created.Id });
    }

    /// <summary>GET /Roles/Edit/{id} — Rol düzenleme formu (Admin).</summary>
    [HttpGet]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Edit(int id)
    {
        var role = _roleService.GetById(id);
        if (role == null) return NotFound();
        ViewBag.Role = role;
        return View(new RoleEditInputModel { Name = role.Name, Code = role.Code, Description = role.Description });
    }

    /// <summary>POST /Roles/Edit/{id} — Rol adı/kod/açıklamasını günceller (Admin).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Edit(int id, RoleEditInputModel input)
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
        TempData["RoleEditSuccess"] = "Rol bilgileri güncellendi.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    /// <summary>GET /Roles/EditPermissions/{id} — Rolün sistem bazlı yetkilerini (RolePermission) düzenleme sayfası (Admin).</summary>
    [HttpGet]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult EditPermissions(int id)
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

    /// <summary>POST /Roles/AddPermission — Role belirtilen sistem için yetki (PermissionType, IsDefault) ekler (Admin).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult AddPermission(int id, RoleAddPermissionInputModel input)
    {
        var role = _roleService.GetById(id);
        if (role == null) return NotFound();
        var rp = _roleService.AddPermissionToRole(id, input.ResourceSystemId, input.PermissionType, input.IsDefault);
        var actorName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
        _auditService.Log(AuditAction.RolePermissionAdded, _currentUser.UserId, actorName, "RolePermission", rp.Id.ToString(), $"Rol: {role.Name}");
        TempData["RolePermissionSuccess"] = "Yetki eklendi.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    /// <summary>POST /Roles/RemovePermission — Rolün belirtilen RolePermission kaydını kaldırır (Admin).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult RemovePermission(int id, int permissionId)
    {
        var role = _roleService.GetById(id);
        if (role == null) return NotFound();
        if (_roleService.RemoveRolePermission(permissionId))
        {
            var actorName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
            _auditService.Log(AuditAction.RolePermissionRemoved, _currentUser.UserId, actorName, "RolePermission", permissionId.ToString(), $"Rol: {role.Name}");
        }
        TempData["RolePermissionSuccess"] = "Yetki kaldırıldı.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    /// <summary>GET /Roles/Delete/{id} — Rol silme onay sayfası (Admin).</summary>
    [HttpGet]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Delete(int id)
    {
        var role = _roleService.GetById(id);
        if (role == null) return NotFound();
        ViewBag.Role = role;
        ViewBag.DeleteFailed = false;
        return View();
    }

    /// <summary>POST /Roles/Delete/{id} — Rolü siler; personelde atanmışsa silinmez (Admin).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Delete")]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult DeletePost(int id)
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
    public int ResourceSystemId { get; set; }
    public PermissionType PermissionType { get; set; }
    public bool IsDefault { get; set; } = true;
}
