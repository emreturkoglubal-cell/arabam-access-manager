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
            responsible_department_id AS ResponsibleDepartmentId, description AS Description, unit_cost AS UnitCost, unit_cost_currency AS UnitCostCurrency FROM resource_systems ORDER BY name";
        var list = conn.Query<ResourceSystem>(sql).ToList();
        FillOwnerIds(conn, list);
        return list;
    }

    public ResourceSystem? GetById(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, name AS Name, code AS Code, system_type AS SystemType, critical_level AS CriticalLevel,
            responsible_department_id AS ResponsibleDepartmentId, description AS Description, unit_cost AS UnitCost, unit_cost_currency AS UnitCostCurrency FROM resource_systems WHERE id = @Id";
        var system = conn.QuerySingleOrDefault<ResourceSystem>(sql, new { Id = id });
        if (system != null) FillOwnerIds(conn, new List<ResourceSystem> { system });
        return system;
    }

    public IReadOnlyList<ResourceSystem> GetByIds(IReadOnlyList<int> ids)
    {
        if (ids == null || ids.Count == 0) return new List<ResourceSystem>();
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, name AS Name, code AS Code, system_type AS SystemType, critical_level AS CriticalLevel,
            responsible_department_id AS ResponsibleDepartmentId, description AS Description, unit_cost AS UnitCost, unit_cost_currency AS UnitCostCurrency FROM resource_systems WHERE id = ANY(@Ids)";
        var list = conn.Query<ResourceSystem>(sql, new { Ids = ids.Distinct().ToList() }).ToList();
        FillOwnerIds(conn, list);
        return list;
    }

    public IReadOnlyList<ResourceSystem> GetByType(SystemType type)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, name AS Name, code AS Code, system_type AS SystemType, critical_level AS CriticalLevel,
            responsible_department_id AS ResponsibleDepartmentId, description AS Description, unit_cost AS UnitCost, unit_cost_currency AS UnitCostCurrency FROM resource_systems WHERE system_type = @Type ORDER BY name";
        var list = conn.Query<ResourceSystem>(sql, new { Type = (short)type }).ToList();
        FillOwnerIds(conn, list);
        return list;
    }

    public IReadOnlyList<ResourceSystem> GetByCriticalLevel(CriticalLevel level)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, name AS Name, code AS Code, system_type AS SystemType, critical_level AS CriticalLevel,
            responsible_department_id AS ResponsibleDepartmentId, description AS Description, unit_cost AS UnitCost, unit_cost_currency AS UnitCostCurrency FROM resource_systems WHERE critical_level = @Level ORDER BY name";
        var list = conn.Query<ResourceSystem>(sql, new { Level = (short)level }).ToList();
        FillOwnerIds(conn, list);
        return list;
    }

    public int Insert(ResourceSystem system)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO resource_systems (name, code, system_type, critical_level, responsible_department_id, description, unit_cost, unit_cost_currency)
            VALUES (@Name, @Code, @SystemType, @CriticalLevel, @ResponsibleDepartmentId, @Description, @UnitCost, @UnitCostCurrency) RETURNING id";
        var id = conn.ExecuteScalar<int>(sql, new {
            system.Name, system.Code, SystemType = (short)system.SystemType, CriticalLevel = (short)system.CriticalLevel,
            system.ResponsibleDepartmentId, system.Description, system.UnitCost, system.UnitCostCurrency
        });
        if (system.OwnerIds.Count > 0)
            SetOwnersInternal(conn, id, system.OwnerIds);
        return id;
    }

    public void Update(ResourceSystem system)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"UPDATE resource_systems SET name=@Name, code=@Code, system_type=@SystemType, critical_level=@CriticalLevel,
            responsible_department_id=@ResponsibleDepartmentId, description=@Description, unit_cost=@UnitCost, unit_cost_currency=@UnitCostCurrency, updated_at=now() WHERE id=@Id";
        conn.Execute(sql, new {
            system.Id, system.Name, system.Code, SystemType = (short)system.SystemType, CriticalLevel = (short)system.CriticalLevel,
            system.ResponsibleDepartmentId, system.Description, system.UnitCost, system.UnitCostCurrency
        });
        SetOwnersInternal(conn, system.Id, system.OwnerIds);
    }

    private static void FillOwnerIds(NpgsqlConnection conn, List<ResourceSystem> systems)
    {
        if (systems.Count == 0) return;
        var ids = systems.Select(s => s.Id).Distinct().ToList();
        var dict = GetOwnerIdsForSystemsInternal(conn, ids);
        foreach (var s in systems)
            s.OwnerIds = dict.TryGetValue(s.Id, out var list) ? list : new List<int>();
    }

    public IReadOnlyList<int> GetOwnerIds(int resourceSystemId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        return GetOwnerIdsInternal(conn, resourceSystemId);
    }

    private static List<int> GetOwnerIdsInternal(NpgsqlConnection conn, int resourceSystemId)
    {
        const string sql = "SELECT personnel_id FROM resource_system_owners WHERE resource_system_id = @Id ORDER BY personnel_id";
        return conn.Query<int>(sql, new { Id = resourceSystemId }).ToList();
    }

    public IReadOnlyDictionary<int, List<int>> GetOwnerIdsForSystems(IReadOnlyList<int> resourceSystemIds)
    {
        if (resourceSystemIds == null || resourceSystemIds.Count == 0)
            return new Dictionary<int, List<int>>();
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        return GetOwnerIdsForSystemsInternal(conn, resourceSystemIds.Distinct().ToList());
    }

    private static IReadOnlyDictionary<int, List<int>> GetOwnerIdsForSystemsInternal(NpgsqlConnection conn, IReadOnlyList<int> ids)
    {
        const string sql = "SELECT resource_system_id, personnel_id FROM resource_system_owners WHERE resource_system_id = ANY(@Ids) ORDER BY resource_system_id, personnel_id";
        var rows = conn.Query<(int resource_system_id, int personnel_id)>(sql, new { Ids = ids }).ToList();
        var dict = new Dictionary<int, List<int>>();
        foreach (var (rsId, pId) in rows)
        {
            if (!dict.ContainsKey(rsId)) dict[rsId] = new List<int>();
            dict[rsId].Add(pId);
        }
        return dict;
    }

    public void SetOwners(int resourceSystemId, IReadOnlyList<int> personnelIds)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        SetOwnersInternal(conn, resourceSystemId, personnelIds ?? Array.Empty<int>());
    }

    private static void SetOwnersInternal(NpgsqlConnection conn, int resourceSystemId, IReadOnlyList<int> personnelIds)
    {
        conn.Execute("DELETE FROM resource_system_owners WHERE resource_system_id = @Id", new { Id = resourceSystemId });
        var distinct = (personnelIds ?? Array.Empty<int>()).Where(id => id > 0).Distinct().ToList();
        if (distinct.Count == 0) return;
        const string sql = "INSERT INTO resource_system_owners (resource_system_id, personnel_id) VALUES (@ResourceSystemId, @PersonnelId)";
        foreach (var pId in distinct)
            conn.Execute(sql, new { ResourceSystemId = resourceSystemId, PersonnelId = pId });
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
