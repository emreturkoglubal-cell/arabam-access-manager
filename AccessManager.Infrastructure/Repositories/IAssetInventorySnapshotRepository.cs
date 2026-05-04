using AccessManager.Application.Dtos;

namespace AccessManager.Infrastructure.Repositories;

public interface IAssetInventorySnapshotRepository
{
    void Upsert(DateTime snapshotMonth, short assetStatus, int assetCount);
    IReadOnlyList<MonthInventoryTotalPair> GetTotalInventoryByMonth(int lastMonths);
}
