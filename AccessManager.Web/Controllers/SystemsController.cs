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
/// Kaynak sistemler (ResourceSystem): liste, oluşturma, düzenleme, silme. Her sistemin adı, kodu, türü (Application/Infrastructure/License), kritiklik seviyesi ve birden fazla sorumlu kişi (OwnerIds) atanabilir.
/// Yetki: Liste/Edit için Admin veya Manager; Create/Delete sadece Admin.
/// </summary>
[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class SystemsController : Controller
{
    private readonly ISystemService _systemService;
    private readonly IPersonnelService _personnelService;
    private readonly IDepartmentService _departmentService;
    private readonly IPersonnelAccessService _accessService;
    private readonly IRoleService _roleService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;

    public SystemsController(
        ISystemService systemService,
        IPersonnelService personnelService,
        IDepartmentService departmentService,
        IPersonnelAccessService accessService,
        IRoleService roleService,
        IAuditService auditService,
        ICurrentUserService currentUser)
    {
        _systemService = systemService;
        _personnelService = personnelService;
        _departmentService = departmentService;
        _accessService = accessService;
        _roleService = roleService;
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

        // Sorumlu kişi isimleri (uygulama bazında birden fazla)
        var allOwnerIds = systems.SelectMany(s => s.OwnerIds).Distinct().ToList();
        var owners = allOwnerIds.Count > 0 ? _personnelService.GetByIds(allOwnerIds) : new List<Personnel>();
        var ownerNameByPersonnelId = owners.ToDictionary(p => p.Id, p => $"{p.FirstName} {p.LastName}");
        var ownerNamesBySystem = new Dictionary<int, List<(int PersonnelId, string Name)>>();
        foreach (var s in systems)
        {
            var list = new List<(int, string)>();
            foreach (var pid in s.OwnerIds)
                if (ownerNameByPersonnelId.TryGetValue(pid, out var name))
                    list.Add((pid, name));
            ownerNamesBySystem[s.Id] = list;
        }

        // Sorumlu departman isimleri
        var deptIds = systems.Where(s => s.ResponsibleDepartmentId.HasValue).Select(s => s.ResponsibleDepartmentId!.Value).Distinct().ToList();
        var allDepts = _departmentService.GetAll();
        var deptNameById = allDepts.ToDictionary(d => d.Id, d => d.Name ?? "—");
        var responsibleDepartmentNames = new Dictionary<int, string>();
        foreach (var s in systems)
        {
            if (s.ResponsibleDepartmentId.HasValue && deptNameById.TryGetValue(s.ResponsibleDepartmentId.Value, out var name))
                responsibleDepartmentNames[s.Id] = name;
        }

        ViewBag.Systems = systems;
        ViewBag.OwnerNamesBySystem = ownerNamesBySystem;
        ViewBag.ResponsibleDepartmentNames = responsibleDepartmentNames;
        ViewBag.AccessCounts = accessCounts;
        return View();
    }

    /// <summary>GET /Systems/Detail/{id} — Uygulama detayı (bilgiler + bu uygulamada aktif yetkisi olan personel listesi, sayfalı).</summary>
    [HttpGet]
    public IActionResult Detail(int id, int page = 1)
    {
        var system = _systemService.GetById(id);
        if (system == null) return NotFound();

        var activeAccesses = _accessService.GetActive().Where(a => a.ResourceSystemId == id).ToList();
        var personnelIds = activeAccesses.Select(a => a.PersonnelId).Distinct().ToList();
        var allPersonnel = personnelIds.Count > 0
            ? _personnelService.GetByIds(personnelIds).Where(p => p.Status == Domain.Enums.PersonnelStatus.Active).OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToList()
            : new List<Personnel>();

        var pageSize = 10;
        if (page < 1) page = 1;
        var totalCount = allPersonnel.Count;
        var totalPages = (totalCount + pageSize - 1) / pageSize;
        var skip = (page - 1) * pageSize;
        var personnelList = allPersonnel.Skip(skip).Take(pageSize).ToList();

        ViewBag.System = system;
        ViewBag.PersonnelList = personnelList;
        ViewBag.PersonnelTotalCount = totalCount;
        ViewBag.PersonnelPageNumber = page;
        ViewBag.PersonnelTotalPages = totalPages;
        ViewBag.PersonnelPageSize = pageSize;

        var roleNames = _roleService.GetAll().ToDictionary(r => r.Id, r => r.Name ?? "—");
        ViewBag.RoleNames = roleNames;
        var managerIds = personnelList.Where(p => p.ManagerId.HasValue).Select(p => p.ManagerId!.Value).Distinct().ToList();
        var managers = managerIds.Count > 0 ? _personnelService.GetByIds(managerIds) : new List<Personnel>();
        ViewBag.ManagerNames = managers.ToDictionary(m => m.Id, m => $"{m.FirstName} {m.LastName}");

        var ownerNames = system.OwnerIds.Count > 0
            ? _personnelService.GetByIds(system.OwnerIds).Select(p => (p.Id, Name: $"{p.FirstName} {p.LastName}")).ToList()
            : new List<(int Id, string Name)>();
        ViewBag.OwnerNames = ownerNames;
        ViewBag.ResponsibleDepartmentName = system.ResponsibleDepartmentId.HasValue
            ? _departmentService.GetById(system.ResponsibleDepartmentId.Value)?.Name ?? "—"
            : "—";

        return View();
    }

    /// <summary>GET /Systems/Create — Yeni kaynak sistem oluşturma formu (Admin).</summary>
    [HttpGet]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public IActionResult Create()
    {
        var personnelList = _personnelService.GetActive();
        ViewBag.PersonnelList = personnelList;
        ViewBag.PersonnelJson = JsonSerializer.Serialize(personnelList.Select(p => new { id = p.Id, firstName = p.FirstName ?? "", lastName = p.LastName ?? "", fullName = $"{p.FirstName ?? ""} {p.LastName ?? ""}".Trim() }).ToList());
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
            OwnerIds = input.OwnerIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>(),
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
        var personnelList = _personnelService.GetActive();
        ViewBag.System = system;
        ViewBag.PersonnelList = personnelList;
        ViewBag.PersonnelJson = JsonSerializer.Serialize(personnelList.Select(p => new { id = p.Id, firstName = p.FirstName ?? "", lastName = p.LastName ?? "", fullName = $"{p.FirstName ?? ""} {p.LastName ?? ""}".Trim() }).ToList());
        ViewBag.OwnerIdsJson = JsonSerializer.Serialize(system.OwnerIds ?? new List<int>());
        ViewBag.Departments = _departmentService.GetAll();
        return View(new SystemEditInputModel
        {
            Name = system.Name,
            Code = system.Code,
            SystemType = system.SystemType,
            CriticalLevel = system.CriticalLevel,
            ResponsibleDepartmentId = system.ResponsibleDepartmentId ?? 0,
            OwnerIds = system.OwnerIds?.ToList() ?? new List<int>(),
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
        system.OwnerIds = input.OwnerIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();
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
