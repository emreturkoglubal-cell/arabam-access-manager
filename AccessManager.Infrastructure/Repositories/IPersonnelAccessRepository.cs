using AccessManager.Domain.Entities;

namespace AccessManager.Infrastructure.Repositories;

public interface IPersonnelAccessRepository
{
    IReadOnlyList<PersonnelAccess> GetByPersonnel(int personnelId);
    IReadOnlyList<PersonnelAccess> GetActive();
    IReadOnlyList<PersonnelAccess> GetExpiringWithinDays(int days);
    IReadOnlyList<PersonnelAccess> GetExceptions();
    void RevokeByPersonnel(int personnelId);
    int Insert(PersonnelAccess access);
    void SetActive(int id, bool isActive);
    PersonnelAccess? GetById(int id);
}
