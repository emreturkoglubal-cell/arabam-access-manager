using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.Application.Interfaces;

public interface IPersonnelAccessService
{
    IReadOnlyList<PersonnelAccess> GetByPersonnel(Guid personnelId);
    IReadOnlyList<PersonnelAccess> GetActive();
    IReadOnlyList<PersonnelAccess> GetExpiringWithinDays(int days);
    IReadOnlyList<PersonnelAccess> GetExceptions();
    void Grant(Guid personnelId, Guid resourceSystemId, PermissionType permissionType, bool isException, DateTime? expiresAt = null, Guid? requestId = null);
    void Revoke(Guid personnelAccessId);
}
