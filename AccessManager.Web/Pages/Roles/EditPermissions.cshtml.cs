using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.UI.Pages.Roles;

[Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
public class EditPermissionsModel : PageModel
{
    private readonly IRoleService _roleService;
    private readonly ISystemService _systemService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;

    public EditPermissionsModel(IRoleService roleService, ISystemService systemService, IAuditService auditService, ICurrentUserService currentUser)
    {
        _roleService = roleService;
        _systemService = systemService;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    public Role? Role { get; set; }
    public IReadOnlyList<RolePermission> Permissions { get; set; } = new List<RolePermission>();
    public IReadOnlyList<ResourceSystem> Systems { get; set; } = new List<ResourceSystem>();
    public Dictionary<int, string> SystemNames { get; set; } = new();

    [BindProperty]
    public AddPermissionInput AddPermission { get; set; } = new();

    public IActionResult OnGet(int id)
    {
        Role = _roleService.GetById(id);
        if (Role == null) return NotFound();
        LoadData(id);
        return Page();
    }

    public IActionResult OnPostAdd(int id)
    {
        Role = _roleService.GetById(id);
        if (Role == null) return NotFound();

        var rp = _roleService.AddPermissionToRole(id, AddPermission.ResourceSystemId, AddPermission.PermissionType, AddPermission.IsDefault);
        var actorId = _currentUser.UserId;
        var actorName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
        _auditService.Log(AuditAction.RolePermissionAdded, actorId, actorName, "RolePermission", rp.Id.ToString(), $"Rol: {Role.Name}");

        return RedirectToPage(new { id });
    }

    public IActionResult OnPostRemove(int id, int permissionId)
    {
        Role = _roleService.GetById(id);
        if (Role == null) return NotFound();

        if (_roleService.RemoveRolePermission(permissionId))
        {
            var actorId = _currentUser.UserId;
            var actorName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
            _auditService.Log(AuditAction.RolePermissionRemoved, actorId, actorName, "RolePermission", permissionId.ToString(), $"Rol: {Role.Name}");
        }
        return RedirectToPage(new { id });
    }

    private void LoadData(int roleId)
    {
        Permissions = _roleService.GetPermissionsByRole(roleId);
        Systems = _systemService.GetAll();
        SystemNames = Systems.ToDictionary(s => s.Id, s => s.Name ?? s.Code ?? s.Id.ToString());
    }

    public class AddPermissionInput
    {
        public int ResourceSystemId { get; set; }
        public PermissionType PermissionType { get; set; }
        public bool IsDefault { get; set; } = true;
    }
}
