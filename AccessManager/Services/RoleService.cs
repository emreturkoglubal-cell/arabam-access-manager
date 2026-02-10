using AccessManager.Data;
using AccessManager.Models;

namespace AccessManager.Services;

public class RoleService : IRoleService
{
    private readonly MockDataStore _store = MockDataStore.Current;

    public IReadOnlyList<Role> GetAll() => _store.Roles.ToList();

    public Role? GetById(Guid id) => _store.Roles.FirstOrDefault(r => r.Id == id);

    public IReadOnlyList<RolePermission> GetPermissionsByRole(Guid roleId) =>
        _store.RolePermissions.Where(rp => rp.RoleId == roleId).ToList();

    public IReadOnlyList<RolePermission> GetAllRolePermissions() => _store.RolePermissions.ToList();
}
