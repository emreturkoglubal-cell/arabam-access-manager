using AccessManager.Models;
using AccessManager.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.Pages.Onboarding;

public class IndexModel : PageModel
{
    private readonly IPersonnelService _personnelService;
    private readonly IDepartmentService _departmentService;
    private readonly IRoleService _roleService;
    private readonly IPersonnelAccessService _accessService;
    private readonly IAuditService _auditService;

    public IndexModel(IPersonnelService personnelService, IDepartmentService departmentService,
        IRoleService roleService, IPersonnelAccessService accessService, IAuditService auditService)
    {
        _personnelService = personnelService;
        _departmentService = departmentService;
        _roleService = roleService;
        _accessService = accessService;
        _auditService = auditService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<Department> Departments { get; set; } = new List<Department>();
    public IReadOnlyList<Role> Roles { get; set; } = new List<Role>();
    public IReadOnlyList<Models.Personnel> Managers { get; set; } = new List<Models.Personnel>();

    public class InputModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Guid DepartmentId { get; set; }
        public string? Position { get; set; }
        public Guid? ManagerId { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Today;
        public Guid? RoleId { get; set; }
    }

    public void OnGet()
    {
        Departments = _departmentService.GetAll();
        Roles = _roleService.GetAll();
        Managers = _personnelService.GetActive();
    }

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(Input.FirstName) || string.IsNullOrWhiteSpace(Input.LastName) || string.IsNullOrWhiteSpace(Input.Email))
        {
            ModelState.AddModelError(string.Empty, "Ad, Soyad ve E-posta zorunludur.");
            OnGet();
            return Page();
        }

        var p = new Models.Personnel
        {
            FirstName = Input.FirstName.Trim(),
            LastName = Input.LastName.Trim(),
            Email = Input.Email.Trim(),
            DepartmentId = Input.DepartmentId,
            Position = Input.Position?.Trim(),
            ManagerId = Input.ManagerId,
            StartDate = Input.StartDate,
            RoleId = Input.RoleId,
            Status = PersonnelStatus.Active
        };
        _personnelService.Add(p);

        if (Input.RoleId.HasValue)
        {
            var rolePerms = _roleService.GetPermissionsByRole(Input.RoleId.Value);
            foreach (var rp in rolePerms)
            {
                _accessService.Grant(p.Id, rp.ResourceSystemId, rp.PermissionType, false, null, null);
            }
        }

        _auditService.Log(AuditAction.PersonnelCreated, null, "Sistem", "Personnel", p.Id.ToString(), $"İşe giriş: {p.FirstName} {p.LastName}");
        return RedirectToPage("/Personnel/Detail", new { id = p.Id });
    }
}
