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
    public IActionResult Index(Guid? departmentId, bool? activeOnly)
    {
        var vm = new PersonnelIndexViewModel
        {
            FilterDepartmentId = departmentId,
            FilterActiveOnly = activeOnly ?? true,
            Departments = _departmentService.GetAll(),
            Roles = _roleService.GetAll()
        };

        var list = vm.FilterActiveOnly == true ? _personnelService.GetActive() : _personnelService.GetAll();
        if (vm.FilterDepartmentId.HasValue)
            list = list.Where(p => p.DepartmentId == vm.FilterDepartmentId.Value).ToList();
        vm.PersonnelList = list;

        foreach (var mId in list.Where(p => p.ManagerId.HasValue).Select(p => p.ManagerId!.Value).Distinct())
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
        if (string.IsNullOrWhiteSpace(input.SicilNo) || string.IsNullOrWhiteSpace(input.FirstName) || string.IsNullOrWhiteSpace(input.LastName) || string.IsNullOrWhiteSpace(input.Email))
        {
            ModelState.AddModelError(string.Empty, "Sicil No, Ad, Soyad ve E-posta zorunludur.");
            ViewBag.Departments = _departmentService.GetAll();
            ViewBag.Roles = _roleService.GetAll();
            ViewBag.Managers = _personnelService.GetActive();
            return View(input);
        }
        if (_personnelService.GetBySicilNo(input.SicilNo) != null)
        {
            ModelState.AddModelError(string.Empty, "Bu sicil numarası zaten kayıtlı.");
            ViewBag.Departments = _departmentService.GetAll();
            ViewBag.Roles = _roleService.GetAll();
            ViewBag.Managers = _personnelService.GetActive();
            return View(input);
        }

        var p = new Personnel
        {
            SicilNo = input.SicilNo.Trim(),
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
    public IActionResult Detail(Guid id)
    {
        var (personnel, accesses) = _personnelService.GetWithAccesses(id);
        if (personnel == null) return NotFound();

        var assetAssignments = _assetService.GetActiveAssignmentsByPersonnel(id).ToList();
        var assetNames = new Dictionary<Guid, string>();
        var assetTypes = new Dictionary<Guid, Domain.Enums.AssetType>();
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
    public IActionResult AddZimmetNote(Guid id, Guid assignmentId, ZimmetNoteInputModel input)
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
    public IActionResult AddNote(Guid id, PersonnelNoteInputModel input)
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
            $"Not eklendi: {personnel.FirstName} {personnel.LastName} ({personnel.SicilNo})");
        TempData["NoteSuccess"] = "Not eklendi.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RevokeAccess(Guid id, Guid accessId)
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
        TempData["RevokeSuccess"] = "Yetki kapatıldı.";
        return RedirectToAction(nameof(Detail), new { id });
    }
}
