using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using AccessManager.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

/// <summary>
/// Kaynak sistemler (ResourceSystem): liste, oluşturma, düzenleme, silme. Her sistemin adı, kodu, türü (Application/Infrastructure/License), kritiklik seviyesi ve isteğe bağlı sistem sahibi (OwnerId) vardır.
/// Yetki: Liste/Edit için Admin veya Manager; Create/Delete sadece Admin.
/// </summary>
[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class SystemsController : Controller
{
    private readonly ISystemService _systemService;
    private readonly IPersonnelService _personnelService;
    private readonly IDepartmentService _departmentService;
    private readonly IPersonnelAccessService _accessService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;

    public SystemsController(
        ISystemService systemService,
        IPersonnelService personnelService,
        IDepartmentService departmentService,
        IPersonnelAccessService accessService,
        IAuditService auditService,
        ICurrentUserService currentUser)
    {
        _systemService = systemService;
        _personnelService = personnelService;
        _departmentService = departmentService;
        _accessService = accessService;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    /// <summary>GET /Systems/Index — Tüm kaynak sistemleri listeler; erişim sayısı ve sistem sahibi adı gösterilir.</summary>
    [HttpGet]
    public IActionResult Index()
    {
        var systems = _systemService.GetAll();

        // Tek sorguda tüm aktif erişimleri al, sayıları bellekte hesapla (N+1 önlemi)
        var activeAccesses = _accessService.GetActive();
        var accessCounts = systems.ToDictionary(s => s.Id, s => activeAccesses.Count(a => a.ResourceSystemId == s.Id));

        // Sahip isimleri: sadece gerekli personel ID'leri için tek sorgu (N+1 önlemi)
        var ownerIds = systems.Where(s => s.OwnerId.HasValue).Select(s => s.OwnerId!.Value).Distinct().ToList();
        var owners = ownerIds.Count > 0 ? _personnelService.GetByIds(ownerIds) : new List<Personnel>();
        var ownerNameByPersonnelId = owners.ToDictionary(p => p.Id, p => $"{p.FirstName} {p.LastName}");
        var ownerNames = new Dictionary<int, string>();
        foreach (var s in systems)
        {
            if (s.OwnerId.HasValue && ownerNameByPersonnelId.TryGetValue(s.OwnerId.Value, out var name))
                ownerNames[s.Id] = name;
            else if (s.OwnerId.HasValue)
                ownerNames[s.Id] = "-";
        }

        ViewBag.Systems = systems;
        ViewBag.OwnerNames = ownerNames;
        ViewBag.AccessCounts = accessCounts;
        return View();
    }

    /// <summary>GET /Systems/Create — Yeni kaynak sistem oluşturma formu (Admin).</summary>
    [HttpGet]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Create()
    {
        ViewBag.PersonnelList = _personnelService.GetActive();
        ViewBag.Departments = _departmentService.GetAll();
        return View(new SystemEditInputModel());
    }

    /// <summary>POST /Systems/Create — Yeni kaynak sistem kaydı oluşturur (Admin).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Create(SystemEditInputModel input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            ModelState.AddModelError(nameof(input.Name), "Sistem adı gerekli.");
            ViewBag.PersonnelList = _personnelService.GetActive();
            ViewBag.Departments = _departmentService.GetAll();
            return View(input);
        }
        var system = new ResourceSystem
        {
            Name = input.Name.Trim(),
            Code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim(),
            SystemType = input.SystemType,
            CriticalLevel = input.CriticalLevel,
            ResponsibleDepartmentId = input.ResponsibleDepartmentId == 0 ? null : input.ResponsibleDepartmentId,
            OwnerId = input.OwnerId == 0 ? null : input.OwnerId,
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim()
        };
        _systemService.Create(system);
        var actorName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
        _auditService.Log(AuditAction.SystemCreated, _currentUser.UserId, actorName, "ResourceSystem", system.Id.ToString(), $"Sistem: {system.Name}");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>GET /Systems/Edit/{id} — Kaynak sistem düzenleme formu (Admin).</summary>
    [HttpGet]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Edit(int id)
    {
        var system = _systemService.GetById(id);
        if (system == null) return NotFound();
        ViewBag.System = system;
        ViewBag.PersonnelList = _personnelService.GetActive();
        ViewBag.Departments = _departmentService.GetAll();
        return View(new SystemEditInputModel
        {
            Name = system.Name,
            Code = system.Code,
            SystemType = system.SystemType,
            CriticalLevel = system.CriticalLevel,
            ResponsibleDepartmentId = system.ResponsibleDepartmentId ?? 0,
            OwnerId = system.OwnerId ?? 0,
            Description = system.Description
        });
    }

    /// <summary>POST /Systems/Edit/{id} — Kaynak sistem bilgilerini günceller (Admin).</summary>
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
            ViewBag.Departments = _departmentService.GetAll();
            return View(input);
        }
        system.Name = input.Name.Trim();
        system.Code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim();
        system.SystemType = input.SystemType;
        system.CriticalLevel = input.CriticalLevel;
        system.ResponsibleDepartmentId = input.ResponsibleDepartmentId == 0 ? null : input.ResponsibleDepartmentId;
        system.OwnerId = input.OwnerId == 0 ? null : input.OwnerId;
        system.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        _systemService.Update(system);
        var actorName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
        _auditService.Log(AuditAction.SystemUpdated, _currentUser.UserId, actorName, "ResourceSystem", system.Id.ToString(), $"Sistem: {system.Name}");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>GET /Systems/Delete/{id} — Sistem silme onay sayfası (Admin).</summary>
    [HttpGet]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Delete(int id)
    {
        var system = _systemService.GetById(id);
        if (system == null) return NotFound();
        ViewBag.System = system;
        return View();
    }

    /// <summary>POST /Systems/Delete/{id} — Kaynak sistem siler; talep/rol/personel erişiminde kullanılıyorsa silinmez (Admin).</summary>
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
