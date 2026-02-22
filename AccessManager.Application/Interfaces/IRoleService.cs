using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.Application.Interfaces;

/// <summary>
/// İş rolleri (Role) ve rol yetkileri (RolePermission): rol CRUD, rol başına sistem yetkisi (PermissionType, IsDefault) ekleme/çıkarma.
/// Personel role atandığında bu yetkiler onboarding veya talep sürecinde PersonnelAccess olarak uygulanır.
/// </summary>
public interface IRoleService
{
    /// <summary>Tüm rolleri döner.</summary>
    IReadOnlyList<Role> GetAll();
    /// <summary>ID ile tek rol; yoksa null.</summary>
    Role? GetById(int id);
    /// <summary>Rolün tüm sistem yetkilerini (RolePermission) döner.</summary>
    IReadOnlyList<RolePermission> GetPermissionsByRole(int roleId);
    /// <summary>Tüm rollerin tüm RolePermission kayıtları (toplu listeleme için).</summary>
    IReadOnlyList<RolePermission> GetAllRolePermissions();

    /// <summary>Yeni rol oluşturur.</summary>
    Role CreateRole(Role role);
    /// <summary>Rol adı, kod, açıklama günceller.</summary>
    void UpdateRole(Role role);
    /// <summary>Rolü siler. Personel bu role atanmışsa silmez, false döner.</summary>
    bool DeleteRole(int roleId);

    RolePermission AddPermissionToRole(int roleId, int resourceSystemId, PermissionType permissionType, bool isDefault = true);
    bool RemoveRolePermission(int rolePermissionId);
}
