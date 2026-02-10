using AccessManager.Data;
using AccessManager.Models;

namespace AccessManager.Services;

public interface IPersonnelAccessService
{
    IReadOnlyList<PersonnelAccess> GetByPersonnel(Guid personnelId);
    IReadOnlyList<PersonnelAccess> GetActive();
    IReadOnlyList<PersonnelAccess> GetExpiringWithinDays(int days);
    IReadOnlyList<PersonnelAccess> GetExceptions();
    void Grant(Guid personnelId, Guid resourceSystemId, PermissionType permissionType, bool isException, DateTime? expiresAt = null, Guid? requestId = null);
    void Revoke(Guid personnelAccessId);
}

public class PersonnelAccessService : IPersonnelAccessService
{
    private readonly MockDataStore _store = MockDataStore.Current;

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
