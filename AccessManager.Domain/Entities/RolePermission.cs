using AccessManager.Domain.Enums;

namespace AccessManager.Domain.Entities;

/// <summary>
/// Rolün bir kaynak sistemdeki varsayılan yetkisi. Role atanan personel bu yetkiyi alır; IsDefault ile rolün o sistemdeki varsayılan yetkisi işaretlenebilir.
/// </summary>
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
