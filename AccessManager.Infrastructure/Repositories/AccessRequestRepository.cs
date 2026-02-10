using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class AccessRequestRepository : IAccessRequestRepository
{
    private readonly string _connectionString;

    public AccessRequestRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IReadOnlyList<AccessRequest> GetAll()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, personnel_id AS PersonnelId, resource_system_id AS ResourceSystemId,
            requested_permission AS RequestedPermission, reason AS Reason, start_date AS StartDate, end_date AS EndDate,
            status AS Status, created_at AS CreatedAt, created_by AS CreatedBy FROM access_requests ORDER BY created_at DESC";
        return conn.Query<AccessRequest>(sql).ToList();
    }

    public IReadOnlyList<AccessRequest> GetByPersonnelId(int personnelId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, personnel_id AS PersonnelId, resource_system_id AS ResourceSystemId,
            requested_permission AS RequestedPermission, reason AS Reason, start_date AS StartDate, end_date AS EndDate,
            status AS Status, created_at AS CreatedAt, created_by AS CreatedBy FROM access_requests WHERE personnel_id = @PersonnelId ORDER BY created_at DESC";
        return conn.Query<AccessRequest>(sql, new { PersonnelId = personnelId }).ToList();
    }

    public IReadOnlyList<AccessRequest> GetByIds(IEnumerable<int> requestIds)
    {
        var ids = requestIds.ToList();
        if (ids.Count == 0) return new List<AccessRequest>();
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, personnel_id AS PersonnelId, resource_system_id AS ResourceSystemId,
            requested_permission AS RequestedPermission, reason AS Reason, start_date AS StartDate, end_date AS EndDate,
            status AS Status, created_at AS CreatedAt, created_by AS CreatedBy FROM access_requests WHERE id = ANY(@Ids)";
        return conn.Query<AccessRequest>(sql, new { Ids = ids }).ToList();
    }

    public AccessRequest? GetById(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, personnel_id AS PersonnelId, resource_system_id AS ResourceSystemId,
            requested_permission AS RequestedPermission, reason AS Reason, start_date AS StartDate, end_date AS EndDate,
            status AS Status, created_at AS CreatedAt, created_by AS CreatedBy FROM access_requests WHERE id = @Id";
        return conn.QuerySingleOrDefault<AccessRequest>(sql, new { Id = id });
    }

    public int Insert(AccessRequest request)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO access_requests (personnel_id, resource_system_id, requested_permission, reason, start_date, end_date, status, created_at, created_by)
            VALUES (@PersonnelId, @ResourceSystemId, @RequestedPermission, @Reason, @StartDate, @EndDate, @Status, @CreatedAt, @CreatedBy) RETURNING id";
        return conn.ExecuteScalar<int>(sql, new {
            request.PersonnelId, request.ResourceSystemId, RequestedPermission = (short)request.RequestedPermission,
            request.Reason, request.StartDate, request.EndDate, Status = (short)request.Status, request.CreatedAt, request.CreatedBy
        });
    }

    public void UpdateStatus(int id, AccessRequestStatus status)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        conn.Execute("UPDATE access_requests SET status = @Status WHERE id = @Id", new { Id = id, Status = (short)status });
    }
}
