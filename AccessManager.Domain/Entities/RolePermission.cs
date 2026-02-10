using AccessManager.Domain.Enums;

namespace AccessManager.Domain.Entities;

public class RolePermission
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int ResourceSystemId { get; set; }
    public PermissionType PermissionType { get; set; }
    public bool IsDefault { get; set; }

    public Role? Role { get; set; }
    public ResourceSystem? ResourceSystem { get; set; }
}
