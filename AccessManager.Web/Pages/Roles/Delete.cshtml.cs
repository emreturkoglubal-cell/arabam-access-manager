using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.UI.Pages.Roles;

[Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
public class DeleteModel : PageModel
{
    private readonly IRoleService _roleService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;

    public DeleteModel(IRoleService roleService, IAuditService auditService, ICurrentUserService currentUser)
    {
        _roleService = roleService;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    public Role? Role { get; set; }
    public bool DeleteFailed { get; set; }
    public string? DeleteFailedMessage { get; set; }

    public IActionResult OnGet(int id)
    {
        Role = _roleService.GetById(id);
        if (Role == null) return NotFound();
        return Page();
    }

    public IActionResult OnPost(int id)
    {
        Role = _roleService.GetById(id);
        if (Role == null) return NotFound();

        var deleted = _roleService.DeleteRole(id);
        if (!deleted)
        {
            DeleteFailed = true;
            DeleteFailedMessage = "Bu rol en az bir personelde atanmış olduğu için silinemiyor. Önce personellerin rol atamasını güncelleyin.";
            return Page();
        }

        var actorId = _currentUser.UserId;
        var actorName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
        _auditService.Log(AuditAction.RoleDeleted, actorId, actorName, "Role", id.ToString(), $"Silinen rol: {Role.Name}");

        return RedirectToPage("Index");
    }
}
