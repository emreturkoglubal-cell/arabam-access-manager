using AccessManager.Domain.Entities;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class TeamRepository : ITeamRepository
{
    private readonly string _connectionString;

    public TeamRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IReadOnlyList<Team> GetAll()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, department_id AS DepartmentId, name AS Name, code AS Code, created_at AS CreatedAt FROM teams ORDER BY name";
        return conn.Query<Team>(sql).ToList();
    }

    public IReadOnlyList<Team> GetByDepartmentId(int departmentId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, department_id AS DepartmentId, name AS Name, code AS Code, created_at AS CreatedAt FROM teams WHERE department_id = @DepartmentId ORDER BY name";
        return conn.Query<Team>(sql, new { DepartmentId = departmentId }).ToList();
    }

    public Team? GetById(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, department_id AS DepartmentId, name AS Name, code AS Code, created_at AS CreatedAt FROM teams WHERE id = @Id";
        return conn.QuerySingleOrDefault<Team>(sql, new { Id = id });
    }

    public int Insert(Team team)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO teams (department_id, name, code) VALUES (@DepartmentId, @Name, @Code) RETURNING id";
        return conn.ExecuteScalar<int>(sql, new { team.DepartmentId, team.Name, team.Code });
    }

    public void Update(Team team)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"UPDATE teams SET department_id = @DepartmentId, name = @Name, code = @Code WHERE id = @Id";
        conn.Execute(sql, new { team.Id, team.DepartmentId, team.Name, team.Code });
    }
}
