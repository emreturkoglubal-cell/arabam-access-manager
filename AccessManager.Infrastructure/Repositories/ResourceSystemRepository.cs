using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class ResourceSystemRepository : IResourceSystemRepository
{
    private readonly string _connectionString;

    public ResourceSystemRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IReadOnlyList<ResourceSystem> GetAll()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, name AS Name, code AS Code, system_type AS SystemType, critical_level AS CriticalLevel,
            owner_id AS OwnerId, description AS Description FROM resource_systems ORDER BY name";
        return conn.Query<ResourceSystem>(sql).ToList();
    }

    public ResourceSystem? GetById(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, name AS Name, code AS Code, system_type AS SystemType, critical_level AS CriticalLevel,
            owner_id AS OwnerId, description AS Description FROM resource_systems WHERE id = @Id";
        return conn.QuerySingleOrDefault<ResourceSystem>(sql, new { Id = id });
    }

    public IReadOnlyList<ResourceSystem> GetByType(SystemType type)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, name AS Name, code AS Code, system_type AS SystemType, critical_level AS CriticalLevel,
            owner_id AS OwnerId, description AS Description FROM resource_systems WHERE system_type = @Type ORDER BY name";
        return conn.Query<ResourceSystem>(sql, new { Type = (short)type }).ToList();
    }

    public IReadOnlyList<ResourceSystem> GetByCriticalLevel(CriticalLevel level)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, name AS Name, code AS Code, system_type AS SystemType, critical_level AS CriticalLevel,
            owner_id AS OwnerId, description AS Description FROM resource_systems WHERE critical_level = @Level ORDER BY name";
        return conn.Query<ResourceSystem>(sql, new { Level = (short)level }).ToList();
    }

    public int Insert(ResourceSystem system)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO resource_systems (name, code, system_type, critical_level, owner_id, description)
            VALUES (@Name, @Code, @SystemType, @CriticalLevel, @OwnerId, @Description) RETURNING id";
        return conn.ExecuteScalar<int>(sql, new {
            system.Name, system.Code, SystemType = (short)system.SystemType, CriticalLevel = (short)system.CriticalLevel,
            system.OwnerId, system.Description
        });
    }

    public void Update(ResourceSystem system)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"UPDATE resource_systems SET name=@Name, code=@Code, system_type=@SystemType, critical_level=@CriticalLevel,
            owner_id=@OwnerId, description=@Description, updated_at=now() WHERE id=@Id";
        conn.Execute(sql, new {
            system.Id, system.Name, system.Code, SystemType = (short)system.SystemType, CriticalLevel = (short)system.CriticalLevel,
            system.OwnerId, system.Description
        });
    }

    public bool ExistsInAccessRequests(int resourceSystemId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        return conn.ExecuteScalar<int>("SELECT 1 FROM access_requests WHERE resource_system_id = @Id LIMIT 1", new { Id = resourceSystemId }) == 1;
    }

    public bool ExistsInRolePermissions(int resourceSystemId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        return conn.ExecuteScalar<int>("SELECT 1 FROM role_permissions WHERE resource_system_id = @Id LIMIT 1", new { Id = resourceSystemId }) == 1;
    }

    public bool ExistsInPersonnelAccesses(int resourceSystemId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        return conn.ExecuteScalar<int>("SELECT 1 FROM personnel_accesses WHERE resource_system_id = @Id LIMIT 1", new { Id = resourceSystemId }) == 1;
    }

    public bool Delete(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        var rows = conn.Execute("DELETE FROM resource_systems WHERE id = @Id", new { Id = id });
        return rows > 0;
    }
}
