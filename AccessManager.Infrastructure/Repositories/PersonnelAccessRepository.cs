using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class PersonnelAccessRepository : IPersonnelAccessRepository
{
    private readonly string _connectionString;

    public PersonnelAccessRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IReadOnlyList<PersonnelAccess> GetByPersonnel(int personnelId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, personnel_id AS PersonnelId, resource_system_id AS ResourceSystemId,
            permission_type AS PermissionType, is_exception AS IsException, granted_at AS GrantedAt, expires_at AS ExpiresAt,
            is_active AS IsActive, granted_by_request_id AS GrantedByRequestId
            FROM personnel_accesses WHERE personnel_id = @PersonnelId ORDER BY granted_at DESC";
        return conn.Query<PersonnelAccess>(sql, new { PersonnelId = personnelId }).ToList();
    }

    public IReadOnlyList<PersonnelAccess> GetActive()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, personnel_id AS PersonnelId, resource_system_id AS ResourceSystemId,
            permission_type AS PermissionType, is_exception AS IsException, granted_at AS GrantedAt, expires_at AS ExpiresAt,
            is_active AS IsActive, granted_by_request_id AS GrantedByRequestId
            FROM personnel_accesses WHERE is_active = true ORDER BY personnel_id, resource_system_id";
        return conn.Query<PersonnelAccess>(sql).ToList();
    }

    public IReadOnlyList<PersonnelAccess> GetExpiringWithinDays(int days)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        var until = DateTime.UtcNow.AddDays(days);
        const string sql = @"SELECT id AS Id, personnel_id AS PersonnelId, resource_system_id AS ResourceSystemId,
            permission_type AS PermissionType, is_exception AS IsException, granted_at AS GrantedAt, expires_at AS ExpiresAt,
            is_active AS IsActive, granted_by_request_id AS GrantedByRequestId
            FROM personnel_accesses WHERE is_active = true AND expires_at IS NOT NULL AND expires_at <= @Until ORDER BY expires_at";
        return conn.Query<PersonnelAccess>(sql, new { Until = until }).ToList();
    }

    public IReadOnlyList<PersonnelAccess> GetExceptions()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, personnel_id AS PersonnelId, resource_system_id AS ResourceSystemId,
            permission_type AS PermissionType, is_exception AS IsException, granted_at AS GrantedAt, expires_at AS ExpiresAt,
            is_active AS IsActive, granted_by_request_id AS GrantedByRequestId
            FROM personnel_accesses WHERE is_active = true AND is_exception = true ORDER BY personnel_id";
        return conn.Query<PersonnelAccess>(sql).ToList();
    }

    public void RevokeByPersonnel(int personnelId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        conn.Execute("UPDATE personnel_accesses SET is_active = false WHERE personnel_id = @PersonnelId", new { PersonnelId = personnelId });
    }

    public int Insert(PersonnelAccess access)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO personnel_accesses (personnel_id, resource_system_id, permission_type, is_exception, granted_at, expires_at, is_active, granted_by_request_id)
            VALUES (@PersonnelId, @ResourceSystemId, @PermissionType, @IsException, @GrantedAt, @ExpiresAt, @IsActive, @GrantedByRequestId) RETURNING id";
        return conn.ExecuteScalar<int>(sql, new {
            access.PersonnelId, access.ResourceSystemId, PermissionType = (short)access.PermissionType, access.IsException,
            access.GrantedAt, access.ExpiresAt, access.IsActive, access.GrantedByRequestId
        });
    }

    public void SetActive(int id, bool isActive)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        conn.Execute("UPDATE personnel_accesses SET is_active = @IsActive WHERE id = @Id", new { Id = id, IsActive = isActive });
    }

    public PersonnelAccess? GetById(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, personnel_id AS PersonnelId, resource_system_id AS ResourceSystemId,
            permission_type AS PermissionType, is_exception AS IsException, granted_at AS GrantedAt, expires_at AS ExpiresAt,
            is_active AS IsActive, granted_by_request_id AS GrantedByRequestId FROM personnel_accesses WHERE id = @Id";
        return conn.QuerySingleOrDefault<PersonnelAccess>(sql, new { Id = id });
    }
}
