using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.Application.Interfaces;

public interface IRoleService
{
    IReadOnlyList<Role> GetAll();
    Role? GetById(int id);
    IReadOnlyList<RolePermission> GetPermissionsByRole(int roleId);
    IReadOnlyList<RolePermission> GetAllRolePermissions();

    Role CreateRole(Role role);
    void UpdateRole(Role role);
    /// <summary>Rolü siler. Personel bu role atanmışsa silmez, false döner.</summary>
    bool DeleteRole(int roleId);

    RolePermission AddPermissionToRole(int roleId, int resourceSystemId, PermissionType permissionType, bool isDefault = true);
    bool RemoveRolePermission(int rolePermissionId);
}
