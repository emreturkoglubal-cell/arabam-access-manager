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
            status AS Status, notes AS Notes, purchase_date AS PurchaseDate, created_at AS CreatedAt FROM assets ORDER BY name";
        return conn.Query<Asset>(sql).ToList();
    }

    public IReadOnlyList<Asset> GetByStatus(AssetStatus status)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, asset_type AS AssetType, name AS Name, serial_number AS SerialNumber, brand_model AS BrandModel,
            status AS Status, notes AS Notes, purchase_date AS PurchaseDate, created_at AS CreatedAt FROM assets WHERE status = @Status ORDER BY name";
        return conn.Query<Asset>(sql, new { Status = (short)status }).ToList();
    }

    public IReadOnlyList<Asset> GetByType(AssetType type)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, asset_type AS AssetType, name AS Name, serial_number AS SerialNumber, brand_model AS BrandModel,
            status AS Status, notes AS Notes, purchase_date AS PurchaseDate, created_at AS CreatedAt FROM assets WHERE asset_type = @Type ORDER BY name";
        return conn.Query<Asset>(sql, new { Type = (short)type }).ToList();
    }

    public Asset? GetById(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, asset_type AS AssetType, name AS Name, serial_number AS SerialNumber, brand_model AS BrandModel,
            status AS Status, notes AS Notes, purchase_date AS PurchaseDate, created_at AS CreatedAt FROM assets WHERE id = @Id";
        return conn.QuerySingleOrDefault<Asset>(sql, new { Id = id });
    }

    public int Insert(Asset asset)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO assets (asset_type, name, serial_number, brand_model, status, notes, purchase_date)
            VALUES (@AssetType, @Name, @SerialNumber, @BrandModel, @Status, @Notes, @PurchaseDate) RETURNING id";
        return conn.ExecuteScalar<int>(sql, new {
            AssetType = (short)asset.AssetType, asset.Name, asset.SerialNumber, asset.BrandModel, Status = (short)asset.Status,
            asset.Notes, asset.PurchaseDate
        });
    }

    public void Update(Asset asset)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"UPDATE assets SET asset_type=@AssetType, name=@Name, serial_number=@SerialNumber, brand_model=@BrandModel, status=@Status, notes=@Notes, purchase_date=@PurchaseDate, updated_at=now() WHERE id=@Id";
        conn.Execute(sql, new {
            asset.Id, AssetType = (short)asset.AssetType, asset.Name, asset.SerialNumber, asset.BrandModel, Status = (short)asset.Status,
            asset.Notes, asset.PurchaseDate
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
