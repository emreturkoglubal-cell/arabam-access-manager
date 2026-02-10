using AccessManager.Domain.Enums;

namespace AccessManager.Domain.Entities;

public class RolePermission
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public Guid ResourceSystemId { get; set; }
    public PermissionType PermissionType { get; set; }
    public bool IsDefault { get; set; }

    public Role? Role { get; set; }
    public ResourceSystem? ResourceSystem { get; set; }
}
