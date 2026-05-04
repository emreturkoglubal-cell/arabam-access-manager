using System.Globalization;
using AccessManager.Application.Dtos;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class AssetInventorySnapshotRepository : IAssetInventorySnapshotRepository
{
    private readonly string _connectionString;

    public AssetInventorySnapshotRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public void Upsert(DateTime snapshotMonth, short assetStatus, int assetCount)
    {
        var month = new DateTime(snapshotMonth.Year, snapshotMonth.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"
INSERT INTO asset_inventory_snapshots (snapshot_month, asset_status, asset_count)
VALUES (@SnapshotMonth, @AssetStatus, @AssetCount)
ON CONFLICT (snapshot_month, asset_status) DO UPDATE SET
    asset_count = EXCLUDED.asset_count,
    created_at = now()";
        conn.Execute(sql, new { SnapshotMonth = month, AssetStatus = assetStatus, AssetCount = assetCount });
    }

    public IReadOnlyList<MonthInventoryTotalPair> GetTotalInventoryByMonth(int lastMonths)
    {
        if (lastMonths < 1) lastMonths = 1;
        if (lastMonths > 48) lastMonths = 48;
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        var from = DateTime.UtcNow.Date.AddMonths(-lastMonths + 1);
        var fromMonth = new DateTime(from.Year, from.Month, 1);
        const string sql = @"
SELECT snapshot_month AS MonthStart, SUM(asset_count)::int AS Total
FROM asset_inventory_snapshots
WHERE snapshot_month >= @FromMonth
GROUP BY snapshot_month
ORDER BY snapshot_month";
        var rows = conn.Query<(DateTime MonthStart, int Total)>(sql, new { FromMonth = fromMonth }).ToList();
        var ci = CultureInfo.GetCultureInfo("tr-TR");
        return rows.Select(r => new MonthInventoryTotalPair
        {
            Label = r.MonthStart.ToString("MMM yyyy", ci),
            TotalCount = r.Total
        }).ToList();
    }
}
