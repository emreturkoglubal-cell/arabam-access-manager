using System.Globalization;
using AccessManager.Application.Dtos;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class ResourceSystemCostSnapshotRepository : IResourceSystemCostSnapshotRepository
{
    private readonly string _connectionString;

    public ResourceSystemCostSnapshotRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public void Upsert(int resourceSystemId, DateTime snapshotMonth, decimal? unitCost, string? unitCostCurrency, int activeAccessCount, decimal totalCostUsd)
    {
        var month = new DateTime(snapshotMonth.Year, snapshotMonth.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"
INSERT INTO resource_system_cost_snapshots (resource_system_id, snapshot_month, unit_cost, unit_cost_currency, active_access_count, total_cost_usd)
VALUES (@ResourceSystemId, @SnapshotMonth, @UnitCost, @UnitCostCurrency, @ActiveAccessCount, @TotalCostUsd)
ON CONFLICT (resource_system_id, snapshot_month) DO UPDATE SET
    unit_cost = EXCLUDED.unit_cost,
    unit_cost_currency = EXCLUDED.unit_cost_currency,
    active_access_count = EXCLUDED.active_access_count,
    total_cost_usd = EXCLUDED.total_cost_usd,
    created_at = now()";
        conn.Execute(sql, new { ResourceSystemId = resourceSystemId, SnapshotMonth = month, UnitCost = unitCost, UnitCostCurrency = unitCostCurrency, ActiveAccessCount = activeAccessCount, TotalCostUsd = totalCostUsd });
    }

    public IReadOnlyList<MonthTotalUsdPair> GetGrandTotalsByMonth(int lastMonths)
    {
        if (lastMonths < 1) lastMonths = 1;
        if (lastMonths > 48) lastMonths = 48;
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        var from = DateTime.UtcNow.Date.AddMonths(-lastMonths + 1);
        var fromMonth = new DateTime(from.Year, from.Month, 1);
        const string sql = @"
SELECT snapshot_month AS MonthStart, SUM(total_cost_usd) AS TotalUsd
FROM resource_system_cost_snapshots
WHERE snapshot_month >= @FromMonth
GROUP BY snapshot_month
ORDER BY snapshot_month";
        var rows = conn.Query<(DateTime MonthStart, decimal TotalUsd)>(sql, new { FromMonth = fromMonth }).ToList();
        var ci = CultureInfo.GetCultureInfo("tr-TR");
        return rows.Select(r => new MonthTotalUsdPair
        {
            Label = r.MonthStart.ToString("MMM yyyy", ci),
            TotalUsd = r.TotalUsd
        }).ToList();
    }

    public IReadOnlyList<SystemMonthCostPoint> GetBySystemId(int resourceSystemId, int lastMonths)
    {
        if (lastMonths < 1) lastMonths = 1;
        if (lastMonths > 48) lastMonths = 48;
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        var from = DateTime.UtcNow.Date.AddMonths(-lastMonths + 1);
        var fromMonth = new DateTime(from.Year, from.Month, 1);
        const string sql = @"
SELECT snapshot_month AS MonthStart, total_cost_usd AS TotalCostUsd, active_access_count AS ActiveAccessCount
FROM resource_system_cost_snapshots
WHERE resource_system_id = @Id AND snapshot_month >= @FromMonth
ORDER BY snapshot_month";
        var rows = conn.Query<(DateTime MonthStart, decimal TotalCostUsd, int ActiveAccessCount)>(sql, new { Id = resourceSystemId, FromMonth = fromMonth }).ToList();
        var ci = CultureInfo.GetCultureInfo("tr-TR");
        return rows.Select(r => new SystemMonthCostPoint
        {
            MonthStart = r.MonthStart,
            Label = r.MonthStart.ToString("MMM yyyy", ci),
            TotalCostUsd = r.TotalCostUsd,
            ActiveAccessCount = r.ActiveAccessCount
        }).ToList();
    }
}
