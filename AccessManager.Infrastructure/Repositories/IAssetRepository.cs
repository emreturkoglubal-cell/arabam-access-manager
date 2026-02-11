using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.Infrastructure.Repositories;

public interface IAssetRepository
{
    IReadOnlyList<Asset> GetAll();
    IReadOnlyList<Asset> GetByStatus(AssetStatus status);
    IReadOnlyList<Asset> GetByType(AssetType type);
    (IReadOnlyList<Asset> Items, int TotalCount) GetPaged(AssetStatus? status, AssetType? type, int page, int pageSize);
    Asset? GetById(int id);
    int Insert(Asset asset);
    void Update(Asset asset);
    bool Delete(int id);
}
