using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.Infrastructure.Repositories;

namespace AccessManager.Infrastructure.Services;

public class SystemService : ISystemService
{
    private readonly IResourceSystemRepository _repo;

    public SystemService(IResourceSystemRepository repo)
    {
        _repo = repo;
    }

    public IReadOnlyList<ResourceSystem> GetAll() => _repo.GetAll();

    public ResourceSystem? GetById(int id) => _repo.GetById(id);

    public IReadOnlyList<ResourceSystem> GetByType(SystemType type) => _repo.GetByType(type);

    public IReadOnlyList<ResourceSystem> GetByCriticalLevel(CriticalLevel level) => _repo.GetByCriticalLevel(level);

    public ResourceSystem Create(ResourceSystem system)
    {
        ArgumentNullException.ThrowIfNull(system);
        system.Id = _repo.Insert(system);
        return system;
    }

    public void Update(ResourceSystem system)
    {
        ArgumentNullException.ThrowIfNull(system);
        _repo.Update(system);
    }

    public bool Delete(int id)
    {
        if (_repo.ExistsInAccessRequests(id) || _repo.ExistsInRolePermissions(id) || _repo.ExistsInPersonnelAccesses(id))
            return false;
        return _repo.Delete(id);
    }
}
