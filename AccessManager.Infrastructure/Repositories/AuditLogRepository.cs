using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly string _connectionString;

    public AuditLogRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public void Insert(AuditLog log)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO audit_logs (actor_id, actor_name, action, target_type, target_id, details, timestamp, ip_address)
            VALUES (@ActorId, @ActorName, @Action, @TargetType, @TargetId, @Details, @Timestamp, @IpAddress)";
        conn.Execute(sql, new {
            log.ActorId, log.ActorName, Action = (short)log.Action, log.TargetType, log.TargetId, log.Details, Timestamp = log.Timestamp, log.IpAddress
        });
    }

    public IReadOnlyList<AuditLog> GetRecent(int count)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, actor_id AS ActorId, actor_name AS ActorName, action AS Action, target_type AS TargetType, target_id AS TargetId, details AS Details, timestamp AS Timestamp, ip_address AS IpAddress
            FROM audit_logs ORDER BY timestamp DESC LIMIT @Count";
        return conn.Query<AuditLog>(sql, new { Count = count }).ToList();
    }

    public IReadOnlyList<AuditLog> GetByTarget(string targetType, string? targetId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        var sql = @"SELECT id AS Id, actor_id AS ActorId, actor_name AS ActorName, action AS Action, target_type AS TargetType, target_id AS TargetId, details AS Details, timestamp AS Timestamp, ip_address AS IpAddress
            FROM audit_logs WHERE target_type = @TargetType";
        if (targetId != null) sql += " AND target_id = @TargetId";
        sql += " ORDER BY timestamp DESC";
        return conn.Query<AuditLog>(sql, new { TargetType = targetType, TargetId = targetId }).ToList();
    }

    public IReadOnlyList<AuditLog> GetByDateRange(DateTime from, DateTime to)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, actor_id AS ActorId, actor_name AS ActorName, action AS Action, target_type AS TargetType, target_id AS TargetId, details AS Details, timestamp AS Timestamp, ip_address AS IpAddress
            FROM audit_logs WHERE timestamp >= @From AND timestamp <= @To ORDER BY timestamp DESC";
        return conn.Query<AuditLog>(sql, new { From = from, To = to }).ToList();
    }
}
