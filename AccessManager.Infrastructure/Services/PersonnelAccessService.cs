using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.Infrastructure.Data;

namespace AccessManager.Infrastructure.Services;

public class PersonnelAccessService : IPersonnelAccessService
{
    private readonly MockDataStore _store;

    public PersonnelAccessService(MockDataStore store)
    {
        _store = store;
    }

    public IReadOnlyList<PersonnelAccess> GetByPersonnel(Guid personnelId) =>
        _store.PersonnelAccesses.Where(a => a.PersonnelId == personnelId).ToList();

    public IReadOnlyList<PersonnelAccess> GetActive() =>
        _store.PersonnelAccesses.Where(a => a.IsActive).ToList();

    public IReadOnlyList<PersonnelAccess> GetExpiringWithinDays(int days)
    {
        var limit = DateTime.UtcNow.AddDays(days);
        return _store.PersonnelAccesses
            .Where(a => a.IsActive && a.ExpiresAt.HasValue && a.ExpiresAt <= limit)
            .ToList();
    }

    public IReadOnlyList<PersonnelAccess> GetExceptions() =>
        _store.PersonnelAccesses.Where(a => a.IsActive && a.IsException).ToList();

    public void Grant(Guid personnelId, Guid resourceSystemId, PermissionType permissionType, bool isException, DateTime? expiresAt = null, Guid? requestId = null)
    {
        _store.PersonnelAccesses.Add(new PersonnelAccess
        {
            Id = Guid.NewGuid(),
            PersonnelId = personnelId,
            ResourceSystemId = resourceSystemId,
            PermissionType = permissionType,
            IsException = isException,
            GrantedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IsActive = true,
            GrantedByRequestId = requestId
        });
    }

    public void Revoke(Guid personnelAccessId)
    {
        var a = _store.PersonnelAccesses.FirstOrDefault(x => x.Id == personnelAccessId);
        if (a != null) a.IsActive = false;
    }
}
