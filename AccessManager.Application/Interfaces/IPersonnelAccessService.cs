using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.Application.Interfaces;

public interface IPersonnelAccessService
{
    IReadOnlyList<PersonnelAccess> GetByPersonnel(int personnelId);
    IReadOnlyList<PersonnelAccess> GetActive();
    IReadOnlyList<PersonnelAccess> GetExpiringWithinDays(int days);
    IReadOnlyList<PersonnelAccess> GetExceptions();
    void Grant(int personnelId, int resourceSystemId, PermissionType permissionType, bool isException, DateTime? expiresAt = null, int? requestId = null);
    void Revoke(int personnelAccessId);
    void Reactivate(int personnelAccessId);
}
