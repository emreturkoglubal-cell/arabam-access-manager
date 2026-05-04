using AccessManager.Domain.Entities;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class PositionTitleTemplateRepository : IPositionTitleTemplateRepository
{
    private readonly string _connectionString;

    public PositionTitleTemplateRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IReadOnlyList<PositionTitleTemplate> GetAll()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, department_id AS DepartmentId, team_id AS TeamId, seniority_level AS SeniorityLevel, title AS Title, created_at AS CreatedAt
            FROM position_title_templates ORDER BY department_id NULLS LAST, team_id NULLS LAST, id";
        return conn.Query<PositionTitleTemplate>(sql).ToList();
    }

    public int Insert(PositionTitleTemplate row)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO position_title_templates (department_id, team_id, seniority_level, title)
            VALUES (@DepartmentId, @TeamId, @SeniorityLevel, @Title) RETURNING id";
        return conn.ExecuteScalar<int>(sql, new
        {
            row.DepartmentId,
            row.TeamId,
            SeniorityLevel = string.IsNullOrWhiteSpace(row.SeniorityLevel) ? null : row.SeniorityLevel.Trim(),
            row.Title
        });
    }

    public void Delete(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        conn.Execute("DELETE FROM position_title_templates WHERE id = @Id", new { Id = id });
    }

    public string? ResolveTitle(int? departmentId, int? teamId, string? seniorityLevel)
    {
        var sen = string.IsNullOrWhiteSpace(seniorityLevel) ? null : seniorityLevel.Trim();
        PositionTitleTemplate? best = null;
        var bestScore = -1;
        foreach (var t in GetAll())
        {
            if (t.DepartmentId.HasValue && t.DepartmentId != departmentId) continue;
            if (t.TeamId.HasValue && t.TeamId != teamId) continue;
            if (!string.IsNullOrEmpty(t.SeniorityLevel) && !string.Equals(t.SeniorityLevel, sen, StringComparison.OrdinalIgnoreCase)) continue;

            var score = (t.DepartmentId.HasValue ? 4 : 0) + (t.TeamId.HasValue ? 2 : 0) + (!string.IsNullOrEmpty(t.SeniorityLevel) ? 1 : 0);
            if (score > bestScore)
            {
                bestScore = score;
                best = t;
            }
        }
        return best?.Title;
    }
}
