using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PersonnelEntity = AccessManager.Domain.Entities.Personnel;

namespace AccessManager.UI.Pages.Personnel;

[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class CreateModel : PageModel
{
    private readonly IPersonnelService _personnelService;
    private readonly IDepartmentService _departmentService;
    private readonly IRoleService _roleService;
    private readonly IAuditService _auditService;

    public CreateModel(IPersonnelService personnelService, IDepartmentService departmentService,
        IRoleService roleService, IAuditService auditService)
    {
        _personnelService = personnelService;
        _departmentService = departmentService;
        _roleService = roleService;
        _auditService = auditService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<Department> Departments { get; set; } = new List<Department>();
    public IReadOnlyList<Role> Roles { get; set; } = new List<Role>();
    public IReadOnlyList<PersonnelEntity> Managers { get; set; } = new List<PersonnelEntity>();

    public class InputModel
    {
        public string SicilNo { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string? Position { get; set; }
        public int? ManagerId { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Today;
        public int? RoleId { get; set; }
    }

    public void OnGet()
    {
        Departments = _departmentService.GetAll();
        Roles = _roleService.GetAll();
        Managers = _personnelService.GetActive();
    }

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(Input.SicilNo) || string.IsNullOrWhiteSpace(Input.FirstName) || string.IsNullOrWhiteSpace(Input.LastName) || string.IsNullOrWhiteSpace(Input.Email))
        {
            ModelState.AddModelError(string.Empty, "Sicil No, Ad, Soyad ve E-posta zorunludur.");
            OnGet();
            return Page();
        }
        if (_personnelService.GetBySicilNo(Input.SicilNo) != null)
        {
            ModelState.AddModelError(string.Empty, "Bu sicil numarası zaten kayıtlı.");
            OnGet();
            return Page();
        }

        var p = new PersonnelEntity
        {
            SicilNo = Input.SicilNo.Trim(),
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
        _auditService.Log(AuditAction.PersonnelCreated, null, "Sistem", "Personnel", p.Id.ToString(), $"{p.FirstName} {p.LastName}");
        return RedirectToPage("Detail", new { id = p.Id });
    }
}
