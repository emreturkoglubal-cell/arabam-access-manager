using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.Infrastructure.Repositories;

public interface IAssetRepository
{
    IReadOnlyList<Asset> GetAll();
    IReadOnlyList<Asset> GetByStatus(AssetStatus status);
    IReadOnlyList<Asset> GetByType(AssetType type);
    (IReadOnlyList<Asset> Items, int TotalCount) GetPaged(AssetStatus? status, AssetType? type, string? search, int page, int pageSize);
    /// <summary>Durum bazlı sayılar (grafik için).</summary>
    IReadOnlyDictionary<AssetStatus, int> GetCountByStatus();
    /// <summary>Amortismanı 30 gün içinde bitecek sayı.</summary>
    int GetCountDepreciationEndingSoon(int withinDays = 30);
    Asset? GetById(int id);
    int Insert(Asset asset);
    void Update(Asset asset);
    bool Delete(int id);
}
