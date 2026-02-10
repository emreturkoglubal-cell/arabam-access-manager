using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.Infrastructure.Data;

namespace AccessManager.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly MockDataStore _store;

    public RoleService(MockDataStore store)
    {
        _store = store;
    }

    public IReadOnlyList<Role> GetAll() => _store.Roles.ToList();

    public Role? GetById(Guid id) => _store.Roles.FirstOrDefault(r => r.Id == id);

    public IReadOnlyList<RolePermission> GetPermissionsByRole(Guid roleId) =>
        _store.RolePermissions.Where(rp => rp.RoleId == roleId).ToList();

    public IReadOnlyList<RolePermission> GetAllRolePermissions() => _store.RolePermissions.ToList();

    public Role CreateRole(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);
        role.Id = Guid.NewGuid();
        _store.Roles.Add(role);
        return role;
    }

    public void UpdateRole(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);
        var idx = _store.Roles.FindIndex(r => r.Id == role.Id);
        if (idx >= 0)
            _store.Roles[idx] = role;
    }

    public bool DeleteRole(Guid roleId)
    {
        if (_store.Personnel.Any(p => p.RoleId == roleId))
            return false;
        var idx = _store.Roles.FindIndex(r => r.Id == roleId);
        if (idx < 0) return true;
        _store.Roles.RemoveAt(idx);
        _store.RolePermissions.RemoveAll(rp => rp.RoleId == roleId);
        return true;
    }

    public RolePermission AddPermissionToRole(Guid roleId, Guid resourceSystemId, PermissionType permissionType, bool isDefault = true)
    {
        if (_store.RolePermissions.Any(rp => rp.RoleId == roleId && rp.ResourceSystemId == resourceSystemId && rp.PermissionType == permissionType))
            return _store.RolePermissions.First(rp => rp.RoleId == roleId && rp.ResourceSystemId == resourceSystemId && rp.PermissionType == permissionType);
        var rp = new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            ResourceSystemId = resourceSystemId,
            PermissionType = permissionType,
            IsDefault = isDefault
        };
        _store.RolePermissions.Add(rp);
        return rp;
    }

    public bool RemoveRolePermission(Guid rolePermissionId)
    {
        var idx = _store.RolePermissions.FindIndex(rp => rp.Id == rolePermissionId);
        if (idx < 0) return false;
        _store.RolePermissions.RemoveAt(idx);
        return true;
    }
}
