using AccessManager.Models;

namespace AccessManager.Services;

public interface ISystemService
{
    IReadOnlyList<ResourceSystem> GetAll();
    ResourceSystem? GetById(Guid id);
    IReadOnlyList<ResourceSystem> GetByType(SystemType type);
    IReadOnlyList<ResourceSystem> GetByCriticalLevel(CriticalLevel level);
}
