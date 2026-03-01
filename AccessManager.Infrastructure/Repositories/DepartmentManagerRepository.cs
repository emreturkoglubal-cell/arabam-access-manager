using AccessManager.Domain.Entities;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class DepartmentManagerRepository : IDepartmentManagerRepository
{
    private readonly string _connectionString;

    public DepartmentManagerRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IReadOnlyList<DepartmentManager> GetByDepartmentId(int departmentId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, department_id AS DepartmentId, personnel_id AS PersonnelId, manager_level AS ManagerLevel, display_order AS DisplayOrder, created_at AS CreatedAt
            FROM department_managers WHERE department_id = @DepartmentId ORDER BY manager_level, display_order, id";
        return conn.Query<DepartmentManager>(sql, new { DepartmentId = departmentId }).ToList();
    }

    public void SetForDepartment(int departmentId, IReadOnlyList<(int PersonnelId, short Level)> managers)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        conn.Execute("DELETE FROM department_managers WHERE department_id = @DepartmentId", new { DepartmentId = departmentId });
        if (managers == null || managers.Count == 0) return;
        const string sql = @"INSERT INTO department_managers (department_id, personnel_id, manager_level, display_order) VALUES (@DepartmentId, @PersonnelId, @ManagerLevel, @DisplayOrder)";
        var order = 0;
        foreach (var m in managers)
        {
            conn.Execute(sql, new { DepartmentId = departmentId, PersonnelId = m.PersonnelId, ManagerLevel = m.Level, DisplayOrder = order++ });
        }
    }
}
