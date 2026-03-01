using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class AssetRepository : IAssetRepository
{
    private readonly string _connectionString;

    public AssetRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IReadOnlyList<Asset> GetAll()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, asset_type AS AssetType, name AS Name, serial_number AS SerialNumber, brand_model AS BrandModel,
            status AS Status, notes AS Notes, purchase_date AS PurchaseDate, purchase_price AS PurchasePrice, purchase_currency AS PurchaseCurrency, depreciation_end_date AS DepreciationEndDate, depreciation_years AS DepreciationYears, spec_ram_gb AS SpecRamGb, spec_storage_gb AS SpecStorageGb, spec_cpu AS SpecCpu, spec_screen_inches AS SpecScreenInches, spec_is_pivot AS SpecIsPivot, created_at AS CreatedAt FROM assets ORDER BY name";
        return conn.Query<Asset>(sql).ToList();
    }

    public IReadOnlyList<Asset> GetByStatus(AssetStatus status)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, asset_type AS AssetType, name AS Name, serial_number AS SerialNumber, brand_model AS BrandModel,
            status AS Status, notes AS Notes, purchase_date AS PurchaseDate, purchase_price AS PurchasePrice, purchase_currency AS PurchaseCurrency, depreciation_end_date AS DepreciationEndDate, depreciation_years AS DepreciationYears, spec_ram_gb AS SpecRamGb, spec_storage_gb AS SpecStorageGb, spec_cpu AS SpecCpu, spec_screen_inches AS SpecScreenInches, spec_is_pivot AS SpecIsPivot, created_at AS CreatedAt FROM assets WHERE status = @Status ORDER BY name";
        return conn.Query<Asset>(sql, new { Status = (short)status }).ToList();
    }

    public IReadOnlyList<Asset> GetByType(AssetType type)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, asset_type AS AssetType, name AS Name, serial_number AS SerialNumber, brand_model AS BrandModel,
            status AS Status, notes AS Notes, purchase_date AS PurchaseDate, purchase_price AS PurchasePrice, purchase_currency AS PurchaseCurrency, depreciation_end_date AS DepreciationEndDate, depreciation_years AS DepreciationYears, spec_ram_gb AS SpecRamGb, spec_storage_gb AS SpecStorageGb, spec_cpu AS SpecCpu, spec_screen_inches AS SpecScreenInches, spec_is_pivot AS SpecIsPivot, created_at AS CreatedAt FROM assets WHERE asset_type = @Type ORDER BY name";
        return conn.Query<Asset>(sql, new { Type = (short)type }).ToList();
    }

    public (IReadOnlyList<Asset> Items, int TotalCount) GetPaged(AssetStatus? status, AssetType? type, string? search, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        var conditions = new List<string> { "1=1" };
        if (status.HasValue) conditions.Add("a.status = @Status");
        if (type.HasValue) conditions.Add("a.asset_type = @Type");
        var fromClause = "FROM assets a";
        var searchPattern = string.IsNullOrWhiteSpace(search) ? null : "%" + search.Trim() + "%";
        if (!string.IsNullOrWhiteSpace(search))
            conditions.Add("(a.name ILIKE @Search OR a.serial_number ILIKE @Search OR a.brand_model ILIKE @Search OR EXISTS (SELECT 1 FROM asset_assignments aa INNER JOIN personnel p ON aa.personnel_id = p.id WHERE aa.asset_id = a.id AND aa.returned_at IS NULL AND (p.first_name ILIKE @Search OR p.last_name ILIKE @Search)))");
        var where = string.Join(" AND ", conditions);
        var baseSql = $"{fromClause} WHERE {where}";
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        var countSql = $"SELECT COUNT(*) {baseSql}";
        var pars = new { Status = status.HasValue ? (short?)status.Value : null, Type = type.HasValue ? (short?)type.Value : null, Search = searchPattern };
        var totalCount = conn.ExecuteScalar<int>(countSql, pars);
        var offset = (page - 1) * pageSize;
        var selectFields = "a.id AS Id, a.asset_type AS AssetType, a.name AS Name, a.serial_number AS SerialNumber, a.brand_model AS BrandModel, a.status AS Status, a.notes AS Notes, a.purchase_date AS PurchaseDate, a.purchase_price AS PurchasePrice, a.purchase_currency AS PurchaseCurrency, a.depreciation_end_date AS DepreciationEndDate, a.depreciation_years AS DepreciationYears, a.spec_ram_gb AS SpecRamGb, a.spec_storage_gb AS SpecStorageGb, a.spec_cpu AS SpecCpu, a.spec_screen_inches AS SpecScreenInches, a.spec_is_pivot AS SpecIsPivot, a.created_at AS CreatedAt";
        var dataSql = $@"SELECT {selectFields} {baseSql} ORDER BY a.name LIMIT @PageSize OFFSET @Offset";
        var items = conn.Query<Asset>(dataSql, new { pars.Status, pars.Type, pars.Search, PageSize = pageSize, Offset = offset }).ToList();
        return (items, totalCount);
    }

    public IReadOnlyDictionary<AssetStatus, int> GetCountByStatus()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = "SELECT status AS Status, COUNT(*) AS Cnt FROM assets GROUP BY status";
        var rows = conn.Query<(short Status, int Cnt)>(sql);
        var dict = new Dictionary<AssetStatus, int>();
        foreach (var r in rows)
            dict[(AssetStatus)r.Status] = r.Cnt;
        return dict;
    }

    public int GetCountDepreciationEndingSoon(int withinDays = 30)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = "SELECT COUNT(*) FROM assets WHERE depreciation_end_date IS NOT NULL AND depreciation_end_date BETWEEN current_date AND current_date + @WithinDays * INTERVAL '1 day'";
        return conn.ExecuteScalar<int>(sql, new { WithinDays = withinDays });
    }

    public Asset? GetById(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, asset_type AS AssetType, name AS Name, serial_number AS SerialNumber, brand_model AS BrandModel,
            status AS Status, notes AS Notes, purchase_date AS PurchaseDate, purchase_price AS PurchasePrice, purchase_currency AS PurchaseCurrency, depreciation_end_date AS DepreciationEndDate, depreciation_years AS DepreciationYears, spec_ram_gb AS SpecRamGb, spec_storage_gb AS SpecStorageGb, spec_cpu AS SpecCpu, spec_screen_inches AS SpecScreenInches, spec_is_pivot AS SpecIsPivot, created_at AS CreatedAt FROM assets WHERE id = @Id";
        return conn.QuerySingleOrDefault<Asset>(sql, new { Id = id });
    }

    public int Insert(Asset asset)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO assets (asset_type, name, serial_number, brand_model, status, notes, purchase_date, purchase_price, purchase_currency, depreciation_end_date, depreciation_years, spec_ram_gb, spec_storage_gb, spec_cpu, spec_screen_inches, spec_is_pivot)
            VALUES (@AssetType, @Name, @SerialNumber, @BrandModel, @Status, @Notes, @PurchaseDate, @PurchasePrice, @PurchaseCurrency, @DepreciationEndDate, @DepreciationYears, @SpecRamGb, @SpecStorageGb, @SpecCpu, @SpecScreenInches, @SpecIsPivot) RETURNING id";
        return conn.ExecuteScalar<int>(sql, new {
            AssetType = (short)asset.AssetType, asset.Name, asset.SerialNumber, asset.BrandModel, Status = (short)asset.Status,
            asset.Notes, asset.PurchaseDate, asset.PurchasePrice, asset.PurchaseCurrency, asset.DepreciationEndDate, asset.DepreciationYears,
            asset.SpecRamGb, asset.SpecStorageGb, asset.SpecCpu, asset.SpecScreenInches, asset.SpecIsPivot
        });
    }

    public void Update(Asset asset)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"UPDATE assets SET asset_type=@AssetType, name=@Name, serial_number=@SerialNumber, brand_model=@BrandModel, status=@Status, notes=@Notes, purchase_date=@PurchaseDate, purchase_price=@PurchasePrice, purchase_currency=@PurchaseCurrency, depreciation_end_date=@DepreciationEndDate, depreciation_years=@DepreciationYears, spec_ram_gb=@SpecRamGb, spec_storage_gb=@SpecStorageGb, spec_cpu=@SpecCpu, spec_screen_inches=@SpecScreenInches, spec_is_pivot=@SpecIsPivot, updated_at=now() WHERE id=@Id";
        conn.Execute(sql, new {
            asset.Id, AssetType = (short)asset.AssetType, asset.Name, asset.SerialNumber, asset.BrandModel, Status = (short)asset.Status,
            asset.Notes, asset.PurchaseDate, asset.PurchasePrice, asset.PurchaseCurrency, asset.DepreciationEndDate, asset.DepreciationYears,
            asset.SpecRamGb, asset.SpecStorageGb, asset.SpecCpu, asset.SpecScreenInches, asset.SpecIsPivot
        });
    }

    public bool Delete(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        var rows = conn.Execute("DELETE FROM assets WHERE id = @Id", new { Id = id });
        return rows > 0;
    }
}
