using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.Infrastructure.Repositories;

public interface IResourceSystemRepository
{
    IReadOnlyList<ResourceSystem> GetAll();
    ResourceSystem? GetById(int id);
    IReadOnlyList<ResourceSystem> GetByType(SystemType type);
    IReadOnlyList<ResourceSystem> GetByCriticalLevel(CriticalLevel level);
    int Insert(ResourceSystem system);
    void Update(ResourceSystem system);
    bool ExistsInAccessRequests(int resourceSystemId);
    bool ExistsInRolePermissions(int resourceSystemId);
    bool ExistsInPersonnelAccesses(int resourceSystemId);
    bool Delete(int id);
}
