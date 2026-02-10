using AccessManager.Domain.Entities;

namespace AccessManager.Infrastructure.Repositories;

public interface IRoleRepository
{
    IReadOnlyList<Role> GetAll();
    Role? GetById(int id);
    IReadOnlyList<RolePermission> GetPermissionsByRoleId(int roleId);
    IReadOnlyList<RolePermission> GetAllRolePermissions();
    int Insert(Role role);
    void Update(Role role);
    bool Delete(int roleId);
    int AddPermission(RolePermission rp);
    bool RemovePermission(int rolePermissionId);
}
