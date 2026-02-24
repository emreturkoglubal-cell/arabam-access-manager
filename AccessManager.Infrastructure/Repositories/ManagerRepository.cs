using AccessManager.Domain.Entities;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class ManagerRepository : IManagerRepository
{
    private readonly string _connectionString;

    public ManagerRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IReadOnlyList<Manager> GetLeafManagers()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"
            SELECT m.id AS Id, m.personnel_id AS PersonnelId, m.level AS Level, m.parent_manager_id AS ParentManagerId, m.is_active AS IsActive, m.created_at AS CreatedAt, m.updated_at AS UpdatedAt
            FROM managers m
            WHERE m.is_active = true AND NOT EXISTS (SELECT 1 FROM managers c WHERE c.parent_manager_id = m.id AND c.is_active = true)
            ORDER BY m.level, m.id";
        return conn.Query<Manager>(sql).ToList();
    }

    public Manager? GetById(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, personnel_id AS PersonnelId, level AS Level, parent_manager_id AS ParentManagerId, is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM managers WHERE id = @Id";
        return conn.QuerySingleOrDefault<Manager>(sql, new { Id = id });
    }

    public Manager? GetByPersonnelId(int personnelId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, personnel_id AS PersonnelId, level AS Level, parent_manager_id AS ParentManagerId, is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM managers WHERE personnel_id = @PersonnelId";
        return conn.QuerySingleOrDefault<Manager>(sql, new { PersonnelId = personnelId });
    }

    public IReadOnlyList<Manager> GetAll()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, personnel_id AS PersonnelId, level AS Level, parent_manager_id AS ParentManagerId, is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM managers ORDER BY level, id";
        return conn.Query<Manager>(sql).ToList();
    }

    public int Insert(Manager manager)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO managers (personnel_id, level, parent_manager_id, is_active, created_at, updated_at)
            VALUES (@PersonnelId, @Level, @ParentManagerId, @IsActive, now(), now()) RETURNING id";
        return conn.ExecuteScalar<int>(sql, new { manager.PersonnelId, manager.Level, manager.ParentManagerId, manager.IsActive });
    }

    public void Update(Manager manager)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"UPDATE managers SET personnel_id = @PersonnelId, level = @Level, parent_manager_id = @ParentManagerId, is_active = @IsActive, updated_at = now() WHERE id = @Id";
        conn.Execute(sql, new { manager.Id, manager.PersonnelId, manager.Level, manager.ParentManagerId, manager.IsActive });
    }

    public void Delete(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        conn.Execute("DELETE FROM managers WHERE id = @Id", new { Id = id });
    }
}
