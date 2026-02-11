using AccessManager.Application;
using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.Infrastructure.Repositories;

namespace AccessManager.Infrastructure.Services;

public class PersonnelAccessService : IPersonnelAccessService
{
    private readonly IPersonnelAccessRepository _repo;

    public PersonnelAccessService(IPersonnelAccessRepository repo)
    {
        _repo = repo;
    }

    public IReadOnlyList<PersonnelAccess> GetByPersonnel(int personnelId) => _repo.GetByPersonnel(personnelId);

    public IReadOnlyList<PersonnelAccess> GetActive() => _repo.GetActive();

    public IReadOnlyList<PersonnelAccess> GetExpiringWithinDays(int days) => _repo.GetExpiringWithinDays(days);

    public IReadOnlyList<PersonnelAccess> GetExceptions() => _repo.GetExceptions();

    public void Grant(int personnelId, int resourceSystemId, PermissionType permissionType, bool isException, DateTime? expiresAt = null, int? requestId = null)
    {
        var access = new PersonnelAccess
        {
            PersonnelId = personnelId,
            ResourceSystemId = resourceSystemId,
            PermissionType = permissionType,
            IsException = isException,
            GrantedAt = SystemTime.Now,
            ExpiresAt = expiresAt,
            IsActive = true,
            GrantedByRequestId = requestId
        };
        access.Id = _repo.Insert(access);
    }

    public void Revoke(int personnelAccessId)
    {
        _repo.SetActive(personnelAccessId, false);
    }

    public void Reactivate(int personnelAccessId)
    {
        _repo.SetActive(personnelAccessId, true);
    }
}
