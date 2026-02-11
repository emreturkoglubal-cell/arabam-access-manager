using System.Data;
using AccessManager.Domain.Entities;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly string _connectionString;

    public DepartmentRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IReadOnlyList<Department> GetAll()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, name AS Name, code AS Code, description AS Description
            FROM departments ORDER BY name";
        return conn.Query<Department>(sql).ToList();
    }

    public Department? GetById(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, name AS Name, code AS Code, description AS Description
            FROM departments WHERE id = @Id";
        return conn.QuerySingleOrDefault<Department>(sql, new { Id = id });
    }

    public int Insert(Department department)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO departments (name, code, description) VALUES (@Name, @Code, @Description) RETURNING id";
        return conn.ExecuteScalar<int>(sql, new { department.Name, department.Code, department.Description });
    }
}
