using AccessManager.Data;
using AccessManager.Models;

namespace AccessManager.Services;

public class SystemService : ISystemService
{
    private readonly MockDataStore _store = MockDataStore.Current;

    public IReadOnlyList<ResourceSystem> GetAll() => _store.ResourceSystems.ToList();

    public ResourceSystem? GetById(Guid id) => _store.ResourceSystems.FirstOrDefault(s => s.Id == id);

    public IReadOnlyList<ResourceSystem> GetByType(SystemType type) =>
        _store.ResourceSystems.Where(s => s.SystemType == type).ToList();

    public IReadOnlyList<ResourceSystem> GetByCriticalLevel(CriticalLevel level) =>
        _store.ResourceSystems.Where(s => s.CriticalLevel == level).ToList();
}
