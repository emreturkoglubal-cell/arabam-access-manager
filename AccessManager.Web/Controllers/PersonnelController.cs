using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using AccessManager.UI.Helpers;
using AccessManager.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

/// <summary>
/// Personel yönetimi: listeleme (filtre: departman, aktif, arama), oluşturma, detay, not ekleme, erişim verme/geri alma, donanım zimmet atama.
/// Yetki: Admin veya Manager.
/// </summary>
[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class PersonnelController : Controller
{
    private readonly IPersonnelService _personnelService;
    private readonly IManagerService _managerService;
    private readonly IDepartmentService _departmentService;
    private readonly IRoleService _roleService;
    private readonly ISystemService _systemService;
    private readonly IAssetService _assetService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;
    private readonly IPersonnelAccessService _personnelAccessService;
    private readonly ICurrencyService _currencyService;
    private readonly ITeamService _teamService;
    private readonly IPersonnelReminderService _reminderService;

    public PersonnelController(
        IPersonnelService personnelService,
        IManagerService managerService,
        IDepartmentService departmentService,
        IRoleService roleService,
        ISystemService systemService,
        IAssetService assetService,
        IAuditService auditService,
        ICurrentUserService currentUser,
        IPersonnelAccessService personnelAccessService,
        ICurrencyService currencyService,
        ITeamService teamService,
        IPersonnelReminderService reminderService)
    {
        _personnelService = personnelService;
        _managerService = managerService;
        _departmentService = departmentService;
        _roleService = roleService;
        _systemService = systemService;
        _assetService = assetService;
        _auditService = auditService;
        _currentUser = currentUser;
        _personnelAccessService = personnelAccessService;
        _currencyService = currencyService;
        _teamService = teamService;
        _reminderService = reminderService;
    }

    /// <summary>GET /Personnel/Index — Personel listesi; departman, durum (tümü/aktif/işten çıkan) ve arama ile filtrelenebilir, sayfalı.</summary>
    [HttpGet]
    public IActionResult Index(int? departmentId, string? statusFilter, string? search, int page = 1, int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;
        if (string.IsNullOrEmpty(statusFilter) || (statusFilter != "active" && statusFilter != "offboarded")) statusFilter = null;

        var paged = _personnelService.GetPaged(departmentId, roleId: null, statusFilter, search, page, pageSize);

        var departments = _departmentService.GetAll();
        var roles = _roleService.GetAll();
        var vm = new PersonnelIndexViewModel
        {
            SearchTerm = search,
            FilterDepartmentId = departmentId,
            FilterStatusFilter = statusFilter ?? "all",
            Departments = departments,
            Roles = roles,
            DepartmentNames = departments.ToDictionary(d => d.Id, d => d.Name ?? "—"),
            RoleNames = roles.ToDictionary(r => r.Id, r => r.Name ?? "—"),
            PersonnelList = paged.Items,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount
        };

        var managerIds = paged.Items.Where(p => p.ManagerId.HasValue).Select(p => p.ManagerId!.Value).Distinct().ToList();
        if (managerIds.Count > 0)
        {
            var managers = _personnelService.GetByIds(managerIds);
            foreach (var m in managers)
                vm.ManagerNames[m.Id] = $"{m.FirstName} {m.LastName}";
        }

        return View(vm);
    }

    /// <summary>GET /Personnel/Create — Yeni personel ekleme formu (departman, rol, yönetici listeleri doldurulur).</summary>
    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Departments = _departmentService.GetAll();
        ViewBag.Roles = _roleService.GetAll();
        ViewBag.Managers = _managerService.GetActiveManagerPersonnel();
        return View(new PersonnelCreateInputModel());
    }

    /// <summary>POST /Personnel/Create — Yeni personel kaydı oluşturur; başarıda listeye yönlendirir.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(PersonnelCreateInputModel input)
    {
        if (string.IsNullOrWhiteSpace(input.FirstName) || string.IsNullOrWhiteSpace(input.LastName) || string.IsNullOrWhiteSpace(input.Email))
        {
            ModelState.AddModelError(string.Empty, "Ad, soyad ve e-posta zorunludur.");
            ViewBag.Departments = _departmentService.GetAll();
            ViewBag.Roles = _roleService.GetAll();
            ViewBag.Managers = _managerService.GetActiveManagerPersonnel();
            return View(input);
        }
        var (phoneValid, phoneError) = PhoneFormatHelper.Validate(input.PhoneNumber);
        if (!phoneValid)
        {
            ModelState.AddModelError(nameof(input.PhoneNumber), phoneError!);
            ViewBag.Departments = _departmentService.GetAll();
            ViewBag.Roles = _roleService.GetAll();
            ViewBag.Managers = _managerService.GetActiveManagerPersonnel();
            return View(input);
        }

        var p = new Personnel
        {
            FirstName = input.FirstName.Trim(),
            LastName = input.LastName.Trim(),
            Email = input.Email.Trim(),
            PhoneNumber = PhoneFormatHelper.NormalizeForSave(input.PhoneNumber),
            DepartmentId = input.DepartmentId,
            Position = input.Position?.Trim(),
            ManagerId = input.ManagerId,
            StartDate = input.StartDate,
            RoleId = input.RoleId,
            Status = PersonnelStatus.Active
        };
        _personnelService.Add(p);
        if (input.IsManager)
            _managerService.SetPersonAsManager(p.Id, true, input.ManagerId);
        _auditService.Log(AuditAction.PersonnelCreated, null, "Sistem", "Personnel", p.Id.ToString(), $"{p.FirstName} {p.LastName}");
        return RedirectToAction(nameof(Detail), new { id = p.Id });
    }

    /// <summary>GET /Personnel/Detail/{id} — Personel detayı: erişimler, donanım zimmetleri, notlar, astlar (sayfalı).</summary>
    [HttpGet]
    public IActionResult Detail(int id, int subordinatesPage = 1)
    {
        var (personnel, accesses) = _personnelService.GetWithAccesses(id);
        if (personnel == null) return NotFound();

        var assetAssignments = _assetService.GetAssignmentsByPersonnel(id).ToList();
        var assetNames = new Dictionary<int, string>();
        var assetTypes = new Dictionary<int, Domain.Enums.AssetType>();
        var assetBrandModels = new Dictionary<int, string>();
        var assetSerialNumbers = new Dictionary<int, string>();
        foreach (var a in assetAssignments)
        {
            var asset = _assetService.GetById(a.AssetId);
            if (asset != null)
            {
                assetNames[a.AssetId] = asset.Name;
                assetTypes[a.AssetId] = asset.AssetType;
                assetBrandModels[a.AssetId] = asset.BrandModel ?? "—";
                assetSerialNumbers[a.AssetId] = asset.SerialNumber ?? "—";
            }
        }

        var vm = new PersonnelDetailViewModel
        {
            Personnel = personnel,
            AccessList = accesses,
            AssetAssignments = assetAssignments,
            AssetNames = assetNames,
            AssetTypes = assetTypes,
            AssetBrandModels = assetBrandModels,
            AssetSerialNumbers = assetSerialNumbers,
            DepartmentName = _departmentService.GetById(personnel.DepartmentId)?.Name,
            RoleName = personnel.RoleId.HasValue ? _roleService.GetById(personnel.RoleId.Value)?.Name : null,
            ManagerName = personnel.ManagerId.HasValue ? _personnelService.GetById(personnel.ManagerId.Value) is { } m ? $"{m.FirstName} {m.LastName}" : null : null,
            TeamName = personnel.TeamId.HasValue ? _teamService.GetById(personnel.TeamId.Value)?.Name : null,
            Reminders = _reminderService.GetByPersonnelId(id)
        };
        var allSystems = _systemService.GetAll().ToList();
        vm.AllSystems = allSystems;
        foreach (var sys in allSystems)
            vm.SystemNames[sys.Id] = sys.Name ?? sys.Code ?? sys.Id.ToString();
        var allOwnerIds = allSystems.SelectMany(s => s.OwnerIds).Distinct().ToList();
        var systemOwners = allOwnerIds.Count > 0 ? _personnelService.GetByIds(allOwnerIds) : new List<Personnel>();
        var systemOwnerNameByPersonnelId = systemOwners.ToDictionary(p => p.Id, p => $"{p.FirstName} {p.LastName}");
        foreach (var sys in allSystems)
        {
            var owners = new List<(int PersonnelId, string Name)>();
            foreach (var pid in sys.OwnerIds)
            {
                if (systemOwnerNameByPersonnelId.TryGetValue(pid, out var ownerName))
                {
                    owners.Add((pid, ownerName));
                    if (!vm.SystemOwnerNames.ContainsKey(sys.Id))
                        vm.SystemOwnerNames[sys.Id] = ownerName;
                    else
                        vm.SystemOwnerNames[sys.Id] += ", " + ownerName;
                }
            }
            vm.SystemOwnersList[sys.Id] = owners;
        }
        var systemDeptIds = allSystems.Where(s => s.ResponsibleDepartmentId.HasValue).Select(s => s.ResponsibleDepartmentId!.Value).Distinct().ToList();
        var allDepts = _departmentService.GetAll();
        var deptNameById = allDepts.ToDictionary(d => d.Id, d => d.Name ?? "—");
        foreach (var sys in allSystems)
        {
            if (sys.ResponsibleDepartmentId.HasValue && deptNameById.TryGetValue(sys.ResponsibleDepartmentId.Value, out var deptName))
                vm.SystemResponsibleDepartmentNames[sys.Id] = deptName;
        }
        var activeSystemIds = accesses.Where(a => a.IsActive).Select(a => a.ResourceSystemId).Distinct().ToHashSet();
        var ratesToUsd = _currencyService.GetRatesToUsd();
        decimal totalUsd = 0;
        foreach (var sys in allSystems.Where(s => activeSystemIds.Contains(s.Id) && s.UnitCost.HasValue))
        {
            var currency = string.IsNullOrWhiteSpace(sys.UnitCostCurrency) ? "TRY" : sys.UnitCostCurrency.Trim().ToUpperInvariant();
            if (ratesToUsd.TryGetValue(currency, out var rate))
                totalUsd += sys.UnitCost!.Value * rate;
        }
        vm.ApplicationCostUsd = totalUsd > 0 ? totalUsd : (decimal?)null;
        vm.Notes = _personnelService.GetNotes(id).ToList();
        foreach (var assignment in assetAssignments)
            vm.AssignmentNotes[assignment.Id] = _assetService.GetNotesForAssignment(assignment.Id).ToList();

        var allSubordinates = _personnelService.GetByManagerId(id).ToList();
        vm.SubordinatesTotalCount = allSubordinates.Count;
        vm.SubordinatesPageSize = 10;
        vm.SubordinatesPage = subordinatesPage < 1 ? 1 : subordinatesPage;
        var skip = (vm.SubordinatesPage - 1) * vm.SubordinatesPageSize;
        vm.Subordinates = allSubordinates.Skip(skip).Take(vm.SubordinatesPageSize).ToList();

        var managersForEditList = _managerService.GetActiveManagerPersonnel().Where(p => p.Id != id).ToList();
        if (personnel.ManagerId.HasValue && managersForEditList.All(p => p.Id != personnel.ManagerId.Value))
        {
            var currentManager = _personnelService.GetById(personnel.ManagerId.Value);
            if (currentManager != null && _managerService.IsPersonManagerActive(currentManager.Id))
                managersForEditList.Add(currentManager);
        }
        ViewBag.ManagersForEdit = managersForEditList;
        ViewBag.TeamsForEdit = _teamService.GetByDepartmentId(personnel.DepartmentId);
        ViewBag.CurrentManagerLevel = personnel.ManagerId.HasValue ? _managerService.GetManagerLevelByPersonnelId(personnel.ManagerId.Value) : (short?)null;
        ViewBag.IsManager = _managerService.IsPersonManagerActive(id);
        ViewBag.Departments = _departmentService.GetAll();
        ViewBag.Roles = _roleService.GetAll();
        ViewBag.FormattedPhone = PhoneFormatHelper.Format(personnel.PhoneNumber);
        return View(vm);
    }

    /// <summary>POST /Personnel/UpdatePersonnel/{id} — Kişisel bilgiler modalından gelen tüm alanları günceller (ID hariç).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdatePersonnel(int id, PersonnelEditInputModel input)
    {
        var personnel = _personnelService.GetById(id);
        if (personnel == null) return NotFound();
        if (input == null || string.IsNullOrWhiteSpace(input.FirstName) || string.IsNullOrWhiteSpace(input.LastName) || string.IsNullOrWhiteSpace(input.Email))
        {
            TempData["PersonnelEditError"] = "Ad, soyad ve e-posta zorunludur.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        if (input.ManagerId.HasValue && input.ManagerId.Value == id)
        {
            TempData["PersonnelEditError"] = "Kişi kendisinin yöneticisi olamaz.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        var (phoneValid, phoneError) = PhoneFormatHelper.Validate(input.PhoneNumber);
        if (!phoneValid)
        {
            TempData["PersonnelEditError"] = phoneError;
            return RedirectToAction(nameof(Detail), new { id });
        }
        personnel.FirstName = input.FirstName.Trim();
        personnel.LastName = input.LastName.Trim();
        personnel.Email = input.Email.Trim();
        personnel.PhoneNumber = PhoneFormatHelper.NormalizeForSave(input.PhoneNumber);
        personnel.DepartmentId = input.DepartmentId;
        personnel.TeamId = input.TeamId;
        personnel.Position = string.IsNullOrWhiteSpace(input.Position) ? null : input.Position.Trim();
        personnel.SeniorityLevel = string.IsNullOrWhiteSpace(input.SeniorityLevel) ? null : input.SeniorityLevel.Trim();
        personnel.ManagerId = input.ManagerId;
        personnel.StartDate = input.StartDate;
        personnel.EndDate = input.EndDate;
        personnel.RoleId = input.RoleId;
        if (input.Status >= 0 && input.Status <= 2)
            personnel.Status = (PersonnelStatus)input.Status;
        _personnelService.Update(personnel);
        _managerService.SetPersonAsManager(id, input.IsManager, input.ManagerId);
        _auditService.Log(AuditAction.PersonnelUpdated, _currentUser.UserId, _currentUser.DisplayName ?? _currentUser.UserName ?? "?", "Personnel", id.ToString(), $"Kişisel bilgiler güncellendi: {personnel.FirstName} {personnel.LastName}");
        TempData["PersonnelEditSuccess"] = "Kişisel bilgiler güncellendi.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    /// <summary>POST /Personnel/UpdateManager/{id} — Personelin yöneticisini ve yönetici seviyesini günceller.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateManager(int id, int? managerId, short level = 1)
    {
        var personnel = _personnelService.GetById(id);
        if (personnel == null) return NotFound();
        if (managerId.HasValue && managerId == id)
        {
            TempData["ManagerUpdateError"] = "Kişi kendisinin yöneticisi olamaz.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        if (level < 1 || level > 4) level = 1;
        _managerService.UpdatePersonnelManager(id, managerId, level);
        _auditService.Log(AuditAction.PersonnelUpdated, _currentUser.UserId, _currentUser.DisplayName ?? _currentUser.UserName ?? "?", "Personnel", id.ToString(), $"Yönetici güncellendi: {personnel.FirstName} {personnel.LastName}");
        TempData["ManagerUpdateSuccess"] = "Yönetici güncellendi.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    /// <summary>POST /Personnel/AddZimmetNote — Personelin bir zimmet kaydına not ekler.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddZimmetNote(int id, int assignmentId, ZimmetNoteInputModel input)
    {
        var personnel = _personnelService.GetById(id);
        if (personnel == null) return NotFound();
        var assignment = _assetService.GetAssignmentById(assignmentId);
        if (assignment == null || assignment.PersonnelId != id)
        {
            TempData["ZimmetNoteError"] = "Zimmet kaydı bulunamadı.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        if (string.IsNullOrWhiteSpace(input?.Content))
        {
            TempData["ZimmetNoteError"] = "Not içeriği boş olamaz.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        _assetService.AddNoteToAssignment(assignmentId, input.Content.Trim(), _currentUser.UserId, _currentUser.DisplayName ?? _currentUser.UserName ?? "?");
        var asset = _assetService.GetById(assignment.AssetId);
        _auditService.Log(
            AuditAction.AssetAssignmentNoteAdded,
            _currentUser.UserId,
            _currentUser.DisplayName ?? _currentUser.UserName ?? "?",
            "AssetAssignment",
            assignmentId.ToString(),
            $"Zimmet notu: {asset?.Name ?? "?"} — {personnel.FirstName} {personnel.LastName}");
        TempData["ZimmetNoteSuccess"] = "Zimmet notu eklendi.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    /// <summary>POST /Personnel/AddReminder — Personel için hatırlatma ekler (tarih + açıklama; ileride mail ile uyarı).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddReminder(int id, DateTime ReminderDate, string Description)
    {
        var personnel = _personnelService.GetById(id);
        if (personnel == null) return NotFound();
        if (string.IsNullOrWhiteSpace(Description))
        {
            TempData["NoteError"] = "Açıklama boş olamaz.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        _reminderService.Create(id, ReminderDate, Description.Trim(), _currentUser.UserId, _currentUser.DisplayName ?? _currentUser.UserName);
        TempData["NoteSuccess"] = "Hatırlatma eklendi.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    /// <summary>POST /Personnel/AddNote — Personel kaydına genel not ekler.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddNote(int id, PersonnelNoteInputModel input)
    {
        var personnel = _personnelService.GetById(id);
        if (personnel == null) return NotFound();
        if (string.IsNullOrWhiteSpace(input?.Content))
        {
            TempData["NoteError"] = "Not içeriği boş olamaz.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        _personnelService.AddNote(id, input.Content.Trim(), _currentUser.UserId, _currentUser.DisplayName ?? _currentUser.UserName ?? "?");
        _auditService.Log(
            AuditAction.PersonnelNoteAdded,
            _currentUser.UserId,
            _currentUser.DisplayName ?? _currentUser.UserName ?? "?",
            "Personnel",
            id.ToString(),
            $"Not eklendi: {personnel.FirstName} {personnel.LastName} (#{personnel.Id})");
        TempData["NoteSuccess"] = "Not eklendi.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    /// <summary>POST /Personnel/GrantAccess — Personel için belirtilen sistemde erişim açar (rol dışı / istisna olarak).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult GrantAccess(int id, int systemId)
    {
        var personnel = _personnelService.GetById(id);
        if (personnel == null) return NotFound();
        var system = _systemService.GetById(systemId);
        if (system == null)
        {
            TempData["GrantError"] = "Uygulama bulunamadı.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        var accesses = _personnelAccessService.GetByPersonnel(id);
        var existing = accesses.FirstOrDefault(a => a.ResourceSystemId == systemId);
        if (existing != null)
        {
            if (existing.IsActive)
            {
                TempData["GrantError"] = "Bu uygulama için zaten yetki açık.";
                return RedirectToAction(nameof(Detail), new { id });
            }
            _personnelAccessService.Reactivate(existing.Id);
            _auditService.Log(
                AuditAction.AccessGranted,
                _currentUser.UserId,
                _currentUser.DisplayName ?? _currentUser.UserName ?? "?",
                "PersonnelAccess",
                existing.Id.ToString(),
                $"{personnel.FirstName} {personnel.LastName} — {system.Name} (yeniden açıldı)");
        }
        else
        {
            _personnelAccessService.Grant(id, systemId, PermissionType.Open, isException: true, expiresAt: null, requestId: null);
            _auditService.Log(
                AuditAction.AccessGranted,
                _currentUser.UserId,
                _currentUser.DisplayName ?? _currentUser.UserName ?? "?",
                "PersonnelAccess",
                systemId.ToString(),
                $"{personnel.FirstName} {personnel.LastName} — {system.Name}");
        }
        return RedirectToAction(nameof(Detail), new { id });
    }

    /// <summary>POST /Personnel/RevokeAccess — Personelin belirtilen erişim kaydını kapatır (pasif yapar).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RevokeAccess(int id, int accessId)
    {
        var personnel = _personnelService.GetById(id);
        if (personnel == null) return NotFound();
        var accesses = _personnelAccessService.GetByPersonnel(id);
        var access = accesses.FirstOrDefault(a => a.Id == accessId);
        if (access == null || !access.IsActive)
        {
            TempData["RevokeError"] = "Yetki bulunamadı veya zaten kapalı.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        var systemName = _systemService.GetById(access.ResourceSystemId)?.Name ?? access.ResourceSystemId.ToString();
        _personnelAccessService.Revoke(accessId);
        _auditService.Log(
            AuditAction.AccessRevoked,
            _currentUser.UserId,
            _currentUser.DisplayName ?? _currentUser.UserName ?? "?",
            "PersonnelAccess",
            accessId.ToString(),
            $"{personnel.FirstName} {personnel.LastName} — {systemName}");
        return RedirectToAction(nameof(Detail), new { id });
    }

    /// <summary>GET /Personnel/AssignAsset/{id} — Personel için donanım zimmetleme formu; müsait donanımlar listelenir.</summary>
    [HttpGet]
    public IActionResult AssignAsset(int id)
    {
        var personnel = _personnelService.GetById(id);
        if (personnel == null) return NotFound();
        var available = _assetService.GetByStatus(AssetStatus.Available).ToList();
        ViewBag.Personnel = personnel;
        ViewBag.AvailableAssets = available;
        return View();
    }

    /// <summary>POST /Personnel/AssignAsset — Seçilen donanımı personel zimmetine verir.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AssignAsset(int id, int assetId, string? notes)
    {
        var personnel = _personnelService.GetById(id);
        if (personnel == null) return NotFound();
        try
        {
            _assetService.Assign(assetId, id, notes, _currentUser.UserId, _currentUser.DisplayName ?? _currentUser.UserName ?? "?");
            TempData["AssignSuccess"] = "Donanım zimmetlendi.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (InvalidOperationException ex)
        {
            TempData["AssignError"] = ex.Message;
            return RedirectToAction(nameof(AssignAsset), new { id });
        }
        catch (ArgumentException ex)
        {
            TempData["AssignError"] = ex.Message;
            return RedirectToAction(nameof(AssignAsset), new { id });
        }
    }
}
