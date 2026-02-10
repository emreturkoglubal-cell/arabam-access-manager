using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.UI.Pages.Roles;

[Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
public class EditModel : PageModel
{
    private readonly IRoleService _roleService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;

    public EditModel(IRoleService roleService, IAuditService auditService, ICurrentUserService currentUser)
    {
        _roleService = roleService;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public Role? Role { get; set; }

    public IActionResult OnGet(Guid id)
    {
        Role = _roleService.GetById(id);
        if (Role == null) return NotFound();
        Input.Name = Role.Name;
        Input.Code = Role.Code;
        Input.Description = Role.Description;
        return Page();
    }

    public IActionResult OnPost(Guid id)
    {
        Role = _roleService.GetById(id);
        if (Role == null) return NotFound();

        if (string.IsNullOrWhiteSpace(Input.Name))
        {
            ModelState.AddModelError(nameof(Input.Name), "Rol adı gerekli.");
            return Page();
        }

        Role.Name = Input.Name.Trim();
        Role.Code = string.IsNullOrWhiteSpace(Input.Code) ? null : Input.Code.Trim();
        Role.Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim();
        _roleService.UpdateRole(Role);

        var actorId = _currentUser.UserId;
        var actorName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
        _auditService.Log(AuditAction.RoleUpdated, actorId, actorName, "Role", Role.Id.ToString(), $"Rol: {Role.Name}");

        return RedirectToPage("Index");
    }

    public class InputModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
    }
}
