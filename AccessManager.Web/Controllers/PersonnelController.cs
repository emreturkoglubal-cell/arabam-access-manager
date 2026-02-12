using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using AccessManager.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class PersonnelController : Controller
{
    private readonly IPersonnelService _personnelService;
    private readonly IDepartmentService _departmentService;
    private readonly IRoleService _roleService;
    private readonly ISystemService _systemService;
    private readonly IAssetService _assetService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;
    private readonly IPersonnelAccessService _personnelAccessService;

    public PersonnelController(
        IPersonnelService personnelService,
        IDepartmentService departmentService,
        IRoleService roleService,
        ISystemService systemService,
        IAssetService assetService,
        IAuditService auditService,
        ICurrentUserService currentUser,
        IPersonnelAccessService personnelAccessService)
    {
        _personnelService = personnelService;
        _departmentService = departmentService;
        _roleService = roleService;
        _systemService = systemService;
        _assetService = assetService;
        _auditService = auditService;
        _currentUser = currentUser;
        _personnelAccessService = personnelAccessService;
    }

    [HttpGet]
    public IActionResult Index(int? departmentId, bool? activeOnly, string? search, int page = 1, int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var paged = _personnelService.GetPaged(departmentId, activeOnly ?? false, search, page, pageSize);

        var vm = new PersonnelIndexViewModel
        {
            SearchTerm = search,
            FilterDepartmentId = departmentId,
            FilterActiveOnly = activeOnly ?? false,
            Departments = _departmentService.GetAll(),
            Roles = _roleService.GetAll(),
            PersonnelList = paged.Items,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount
        };

        foreach (var mId in paged.Items.Where(p => p.ManagerId.HasValue).Select(p => p.ManagerId!.Value).Distinct())
        {
            var m = _personnelService.GetById(mId);
            if (m != null) vm.ManagerNames[mId] = $"{m.FirstName} {m.LastName}";
        }

        return View(vm);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Departments = _departmentService.GetAll();
        ViewBag.Roles = _roleService.GetAll();
        ViewBag.Managers = _personnelService.GetActive();
        return View(new PersonnelCreateInputModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(PersonnelCreateInputModel input)
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
        _auditService.Log(AuditAction.PersonnelCreated, null, "Sistem", "Personnel", p.Id.ToString(), $"{p.FirstName} {p.LastName}");
        return RedirectToAction(nameof(Detail), new { id = p.Id });
    }

    [HttpGet]
    public IActionResult Detail(int id)
    {
        var (personnel, accesses) = _personnelService.GetWithAccesses(id);
        if (personnel == null) return NotFound();

        var assetAssignments = _assetService.GetActiveAssignmentsByPersonnel(id).ToList();
        var assetNames = new Dictionary<int, string>();
        var assetTypes = new Dictionary<int, Domain.Enums.AssetType>();
        foreach (var a in assetAssignments)
        {
            var asset = _assetService.GetById(a.AssetId);
            if (asset != null)
            {
                assetNames[a.AssetId] = asset.Name;
                assetTypes[a.AssetId] = asset.AssetType;
            }
        }

        var vm = new PersonnelDetailViewModel
        {
            Personnel = personnel,
            AccessList = accesses,
            AssetAssignments = assetAssignments,
            AssetNames = assetNames,
            AssetTypes = assetTypes,
            DepartmentName = _departmentService.GetById(personnel.DepartmentId)?.Name,
            RoleName = personnel.RoleId.HasValue ? _roleService.GetById(personnel.RoleId.Value)?.Name : null,
            ManagerName = personnel.ManagerId.HasValue ? _personnelService.GetById(personnel.ManagerId.Value) is { } m ? $"{m.FirstName} {m.LastName}" : null : null
        };
        var allSystems = _systemService.GetAll().ToList();
        vm.AllSystems = allSystems;
        foreach (var sys in allSystems)
            vm.SystemNames[sys.Id] = sys.Name ?? sys.Code ?? sys.Id.ToString();
        vm.Notes = _personnelService.GetNotes(id).ToList();
        foreach (var assignment in assetAssignments)
            vm.AssignmentNotes[assignment.Id] = _assetService.GetNotesForAssignment(assignment.Id).ToList();
        return View(vm);
    }

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

    /// <summary>Müsait donanımlardan seçip bu personel için zimmetler.</summary>
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
