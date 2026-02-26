using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.Infrastructure.Repositories;

public interface IResourceSystemRepository
{
    IReadOnlyList<ResourceSystem> GetAll();
    ResourceSystem? GetById(int id);
    IReadOnlyList<ResourceSystem> GetByIds(IReadOnlyList<int> ids);
    IReadOnlyList<ResourceSystem> GetByType(SystemType type);
    IReadOnlyList<ResourceSystem> GetByCriticalLevel(CriticalLevel level);
    int Insert(ResourceSystem system);
    void Update(ResourceSystem system);
    /// <summary>Belirtilen uygulama için sorumlu personel id listesi.</summary>
    IReadOnlyList<int> GetOwnerIds(int resourceSystemId);
    /// <summary>Birden fazla uygulama için sorumlu personel id listeleri (systemId -> owner id listesi).</summary>
    IReadOnlyDictionary<int, List<int>> GetOwnerIdsForSystems(IReadOnlyList<int> resourceSystemIds);
    /// <summary>Uygulamanın sorumlularını tamamen günceller (önce siler, sonra verilen listeyi ekler).</summary>
    void SetOwners(int resourceSystemId, IReadOnlyList<int> personnelIds);
    bool ExistsInAccessRequests(int resourceSystemId);
    bool ExistsInRolePermissions(int resourceSystemId);
    bool ExistsInPersonnelAccesses(int resourceSystemId);
    bool Delete(int id);
}
