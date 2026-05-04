using AccessManager.Application.Dtos;

namespace AccessManager.Infrastructure.Repositories;

public interface IResourceSystemCostSnapshotRepository
{
    void Upsert(int resourceSystemId, DateTime snapshotMonth, decimal? unitCost, string? unitCostCurrency, int activeAccessCount, decimal totalCostUsd);
    IReadOnlyList<MonthTotalUsdPair> GetGrandTotalsByMonth(int lastMonths);
    IReadOnlyList<SystemMonthCostPoint> GetBySystemId(int resourceSystemId, int lastMonths);
}
