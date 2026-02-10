using AccessManager.Domain.Entities;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class ApprovalStepRepository : IApprovalStepRepository
{
    private readonly string _connectionString;

    public ApprovalStepRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IReadOnlyList<ApprovalStep> GetByAccessRequestId(int accessRequestId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, access_request_id AS AccessRequestId, step_name AS StepName,
            approved_by AS ApprovedBy, approved_by_name AS ApprovedByName, approved_at AS ApprovedAt, approved AS Approved, comment AS Comment, ""order"" AS ""Order""
            FROM approval_steps WHERE access_request_id = @AccessRequestId ORDER BY ""order""";
        return conn.Query<ApprovalStep>(sql, new { AccessRequestId = accessRequestId }).ToList();
    }

    public void Insert(ApprovalStep step)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO approval_steps (access_request_id, step_name, approved_by, approved_by_name, approved_at, approved, comment, ""order"")
            VALUES (@AccessRequestId, @StepName, @ApprovedBy, @ApprovedByName, @ApprovedAt, @Approved, @Comment, @Order)";
        conn.Execute(sql, new {
            step.AccessRequestId, step.StepName, step.ApprovedBy, step.ApprovedByName, step.ApprovedAt, step.Approved, step.Comment, Order = step.Order
        });
    }

    public ApprovalStep? GetStep(int accessRequestId, string stepName)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, access_request_id AS AccessRequestId, step_name AS StepName,
            approved_by AS ApprovedBy, approved_by_name AS ApprovedByName, approved_at AS ApprovedAt, approved AS Approved, comment AS Comment, ""order"" AS ""Order""
            FROM approval_steps WHERE access_request_id = @AccessRequestId AND step_name = @StepName";
        return conn.QuerySingleOrDefault<ApprovalStep>(sql, new { AccessRequestId = accessRequestId, StepName = stepName });
    }

    public void UpdateApproval(int id, int? approvedBy, string? approvedByName, DateTime? approvedAt, bool? approved, string? comment)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"UPDATE approval_steps SET approved_by = @ApprovedBy, approved_by_name = @ApprovedByName, approved_at = @ApprovedAt, approved = @Approved, comment = @Comment WHERE id = @Id";
        conn.Execute(sql, new { Id = id, ApprovedBy = approvedBy, ApprovedByName = approvedByName, ApprovedAt = approvedAt, Approved = approved, Comment = comment });
    }
}
