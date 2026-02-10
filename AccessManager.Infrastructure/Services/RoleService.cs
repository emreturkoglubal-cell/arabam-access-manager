using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.Infrastructure.Repositories;

namespace AccessManager.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _repo;
    private readonly IPersonnelRepository _personnelRepo;

    public RoleService(IRoleRepository repo, IPersonnelRepository personnelRepo)
    {
        _repo = repo;
        _personnelRepo = personnelRepo;
    }

    public IReadOnlyList<Role> GetAll() => _repo.GetAll();

    public Role? GetById(int id) => _repo.GetById(id);

    public IReadOnlyList<RolePermission> GetPermissionsByRole(int roleId) => _repo.GetPermissionsByRoleId(roleId);

    public IReadOnlyList<RolePermission> GetAllRolePermissions() => _repo.GetAllRolePermissions();

    public Role CreateRole(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);
        role.Id = _repo.Insert(role);
        return role;
    }

    public void UpdateRole(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);
        _repo.Update(role);
    }

    public bool DeleteRole(int roleId)
    {
        if (_personnelRepo.GetAll().Any(p => p.RoleId == roleId))
            return false;
        return _repo.Delete(roleId);
    }

    public RolePermission AddPermissionToRole(int roleId, int resourceSystemId, PermissionType permissionType, bool isDefault = true)
    {
        var existing = _repo.GetPermissionsByRoleId(roleId).FirstOrDefault(rp => rp.ResourceSystemId == resourceSystemId && rp.PermissionType == permissionType);
        if (existing != null) return existing;
        var rp = new RolePermission
        {
            RoleId = roleId,
            ResourceSystemId = resourceSystemId,
            PermissionType = permissionType,
            IsDefault = isDefault
        };
        rp.Id = _repo.AddPermission(rp);
        return rp;
    }

    public bool RemoveRolePermission(int rolePermissionId) => _repo.RemovePermission(rolePermissionId);
}
