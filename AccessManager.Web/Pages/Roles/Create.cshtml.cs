using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.UI.Pages.Roles;

[Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
public class CreateModel : PageModel
{
    private readonly IRoleService _roleService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;

    public CreateModel(IRoleService roleService, IAuditService auditService, ICurrentUserService currentUser)
    {
        _roleService = roleService;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IActionResult OnGet() => Page();

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(Input.Name))
        {
            ModelState.AddModelError(nameof(Input.Name), "Rol adı gerekli.");
            return Page();
        }

        var role = new Role
        {
            Name = Input.Name.Trim(),
            Code = string.IsNullOrWhiteSpace(Input.Code) ? null : Input.Code.Trim(),
            Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim()
        };
        _roleService.CreateRole(role);

        var actorId = _currentUser.UserId;
        var actorName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
        _auditService.Log(AuditAction.RoleCreated, actorId, actorName, "Role", role.Id.ToString(), $"Rol: {role.Name}");

        return RedirectToPage("Index");
    }

    public class InputModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
    }
}
