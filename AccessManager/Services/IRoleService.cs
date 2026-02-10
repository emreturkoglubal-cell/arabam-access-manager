using AccessManager.Models;

namespace AccessManager.Services;

public interface IRoleService
{
    IReadOnlyList<Role> GetAll();
    Role? GetById(Guid id);
    IReadOnlyList<RolePermission> GetPermissionsByRole(Guid roleId);
    IReadOnlyList<RolePermission> GetAllRolePermissions();
}
