using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.Infrastructure.Repositories;

public interface IAssetRepository
{
    IReadOnlyList<Asset> GetAll();
    IReadOnlyList<Asset> GetByStatus(AssetStatus status);
    IReadOnlyList<Asset> GetByType(AssetType type);
    Asset? GetById(int id);
    int Insert(Asset asset);
    void Update(Asset asset);
    bool Delete(int id);
}
