using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.Infrastructure.Data;

namespace AccessManager.Infrastructure.Services;

public class SystemService : ISystemService
{
    private readonly MockDataStore _store;

    public SystemService(MockDataStore store)
    {
        _store = store;
    }

    public IReadOnlyList<ResourceSystem> GetAll() => _store.ResourceSystems.ToList();

    public ResourceSystem? GetById(Guid id) => _store.ResourceSystems.FirstOrDefault(s => s.Id == id);

    public IReadOnlyList<ResourceSystem> GetByType(SystemType type) =>
        _store.ResourceSystems.Where(s => s.SystemType == type).ToList();

    public IReadOnlyList<ResourceSystem> GetByCriticalLevel(CriticalLevel level) =>
        _store.ResourceSystems.Where(s => s.CriticalLevel == level).ToList();

    public ResourceSystem Create(ResourceSystem system)
    {
        ArgumentNullException.ThrowIfNull(system);
        system.Id = Guid.NewGuid();
        _store.ResourceSystems.Add(system);
        return system;
    }

    public void Update(ResourceSystem system)
    {
        ArgumentNullException.ThrowIfNull(system);
        var idx = _store.ResourceSystems.FindIndex(s => s.Id == system.Id);
        if (idx >= 0)
            _store.ResourceSystems[idx] = system;
    }

    public bool Delete(Guid id)
    {
        if (_store.AccessRequests.Any(r => r.ResourceSystemId == id))
            return false;
        if (_store.RolePermissions.Any(rp => rp.ResourceSystemId == id))
            return false;
        if (_store.PersonnelAccesses.Any(pa => pa.ResourceSystemId == id))
            return false;
        var idx = _store.ResourceSystems.FindIndex(s => s.Id == id);
        if (idx < 0) return true;
        _store.ResourceSystems.RemoveAt(idx);
        return true;
    }
}
