using AccessManager.Application.Interfaces;
using AccessManager.Domain.Enums;
using AccessManager.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace AccessManager.Infrastructure.Services;

public class DailyMaintenanceRunner : IDailyMaintenanceRunner
{
    private readonly IResourceSystemRepository _systemRepo;
    private readonly IPersonnelAccessRepository _accessRepo;
    private readonly ICurrencyService _currencyService;
    private readonly IResourceSystemCostSnapshotRepository _costSnapshotRepo;
    private readonly IAssetRepository _assetRepo;
    private readonly IAssetInventorySnapshotRepository _inventorySnapshotRepo;
    private readonly ILogger<DailyMaintenanceRunner> _logger;

    public DailyMaintenanceRunner(
        IResourceSystemRepository systemRepo,
        IPersonnelAccessRepository accessRepo,
        ICurrencyService currencyService,
        IResourceSystemCostSnapshotRepository costSnapshotRepo,
        IAssetRepository assetRepo,
        IAssetInventorySnapshotRepository inventorySnapshotRepo,
        ILogger<DailyMaintenanceRunner> logger)
    {
        _systemRepo = systemRepo;
        _accessRepo = accessRepo;
        _currencyService = currencyService;
        _costSnapshotRepo = costSnapshotRepo;
        _assetRepo = assetRepo;
        _inventorySnapshotRepo = inventorySnapshotRepo;
        _logger = logger;
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        var tz = GetTurkeyTimeZone();
        var nowTr = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var snapshotMonth = new DateTime(nowTr.Year, nowTr.Month, 1);

        UpsertSystemCostSnapshots(snapshotMonth);
        UpsertAssetInventorySnapshots(snapshotMonth);

        return Task.CompletedTask;
    }

    private void UpsertSystemCostSnapshots(DateTime snapshotMonth)
    {
        var systems = _systemRepo.GetAll();
        var counts = _accessRepo.GetActiveAccessCountByResourceSystem();
        var rates = _currencyService.GetRatesToUsd();

        foreach (var sys in systems)
        {
            counts.TryGetValue(sys.Id, out var cnt);
            decimal totalUsd = 0;
            decimal? unit = sys.UnitCost;
            string? cur = string.IsNullOrWhiteSpace(sys.UnitCostCurrency) ? "TRY" : sys.UnitCostCurrency.Trim().ToUpperInvariant();
            if (unit.HasValue && cnt > 0 && rates.TryGetValue(cur, out var rate))
                totalUsd = unit.Value * rate * cnt;

            _costSnapshotRepo.Upsert(sys.Id, snapshotMonth, unit, cur, cnt, totalUsd);
        }

        _logger.LogInformation("DailyMaintenance: resource_system_cost_snapshots güncellendi ({Count} sistem, ay {Month:yyyy-MM}).", systems.Count, snapshotMonth);
    }

    private void UpsertAssetInventorySnapshots(DateTime snapshotMonth)
    {
        var byStatus = _assetRepo.GetCountByStatus();
        foreach (AssetStatus st in Enum.GetValues<AssetStatus>())
        {
            var c = byStatus.TryGetValue(st, out var n) ? n : 0;
            _inventorySnapshotRepo.Upsert(snapshotMonth, (short)st, c);
        }

        _logger.LogInformation("DailyMaintenance: asset_inventory_snapshots güncellendi (ay {Month:yyyy-MM}).", snapshotMonth);
    }

    private static TimeZoneInfo GetTurkeyTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        }
        catch
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        }
    }
}
