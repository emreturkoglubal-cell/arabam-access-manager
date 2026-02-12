using AccessManager.Domain.Entities;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class AssetAssignmentRepository : IAssetAssignmentRepository
{
    private readonly string _connectionString;

    public AssetAssignmentRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IReadOnlyList<AssetAssignment> GetByAssetId(int assetId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, asset_id AS AssetId, personnel_id AS PersonnelId, assigned_at AS AssignedAt,
            assigned_by_user_id AS AssignedByUserId, assigned_by_user_name AS AssignedByUserName, returned_at AS ReturnedAt, return_condition AS ReturnCondition, notes AS Notes
            FROM asset_assignments WHERE asset_id = @AssetId ORDER BY assigned_at DESC";
        return conn.Query<AssetAssignment>(sql, new { AssetId = assetId }).ToList();
    }

    public IReadOnlyList<AssetAssignment> GetByPersonnelId(int personnelId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, asset_id AS AssetId, personnel_id AS PersonnelId, assigned_at AS AssignedAt,
            assigned_by_user_id AS AssignedByUserId, assigned_by_user_name AS AssignedByUserName, returned_at AS ReturnedAt, return_condition AS ReturnCondition, notes AS Notes
            FROM asset_assignments WHERE personnel_id = @PersonnelId ORDER BY assigned_at DESC";
        return conn.Query<AssetAssignment>(sql, new { PersonnelId = personnelId }).ToList();
    }

    public AssetAssignment? GetActiveByAssetId(int assetId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, asset_id AS AssetId, personnel_id AS PersonnelId, assigned_at AS AssignedAt,
            assigned_by_user_id AS AssignedByUserId, assigned_by_user_name AS AssignedByUserName, returned_at AS ReturnedAt, return_condition AS ReturnCondition, notes AS Notes
            FROM asset_assignments WHERE asset_id = @AssetId AND returned_at IS NULL LIMIT 1";
        return conn.QuerySingleOrDefault<AssetAssignment>(sql, new { AssetId = assetId });
    }

    public IReadOnlyList<AssetAssignment> GetActiveByAssetIds(IReadOnlyList<int> assetIds)
    {
        if (assetIds == null || assetIds.Count == 0) return new List<AssetAssignment>();
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, asset_id AS AssetId, personnel_id AS PersonnelId, assigned_at AS AssignedAt,
            assigned_by_user_id AS AssignedByUserId, assigned_by_user_name AS AssignedByUserName, returned_at AS ReturnedAt, return_condition AS ReturnCondition, notes AS Notes
            FROM asset_assignments WHERE asset_id = ANY(@AssetIds) AND returned_at IS NULL";
        return conn.Query<AssetAssignment>(sql, new { AssetIds = assetIds.Distinct().ToList() }).ToList();
    }

    public AssetAssignment? GetById(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, asset_id AS AssetId, personnel_id AS PersonnelId, assigned_at AS AssignedAt,
            assigned_by_user_id AS AssignedByUserId, assigned_by_user_name AS AssignedByUserName, returned_at AS ReturnedAt, return_condition AS ReturnCondition, notes AS Notes
            FROM asset_assignments WHERE id = @Id";
        return conn.QuerySingleOrDefault<AssetAssignment>(sql, new { Id = id });
    }

    public int Insert(AssetAssignment assignment)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO asset_assignments (asset_id, personnel_id, assigned_at, assigned_by_user_id, assigned_by_user_name, returned_at, return_condition, notes)
            VALUES (@AssetId, @PersonnelId, @AssignedAt, @AssignedByUserId, @AssignedByUserName, @ReturnedAt, @ReturnCondition, @Notes) RETURNING id";
        return conn.ExecuteScalar<int>(sql, new {
            assignment.AssetId, assignment.PersonnelId, assignment.AssignedAt, assignment.AssignedByUserId, assignment.AssignedByUserName,
            assignment.ReturnedAt, assignment.ReturnCondition, assignment.Notes
        });
    }

    public void SetReturned(int id, DateTime returnedAt, string? returnCondition, string? notes)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        conn.Execute("UPDATE asset_assignments SET returned_at = @ReturnedAt, return_condition = @ReturnCondition, notes = COALESCE(@Notes, notes) WHERE id = @Id",
            new { Id = id, ReturnedAt = returnedAt, ReturnCondition = returnCondition, Notes = notes });
    }

    public void AddNote(AssetAssignmentNote note)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO asset_assignment_notes (asset_assignment_id, content, created_at, created_by_user_id, created_by_user_name)
            VALUES (@AssetAssignmentId, @Content, @CreatedAt, @CreatedByUserId, @CreatedByUserName)";
        conn.Execute(sql, new { note.AssetAssignmentId, note.Content, note.CreatedAt, note.CreatedByUserId, note.CreatedByUserName });
    }

    public IReadOnlyList<AssetAssignmentNote> GetNotesByAssignmentId(int assignmentId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, asset_assignment_id AS AssetAssignmentId, content AS Content, created_at AS CreatedAt, created_by_user_id AS CreatedByUserId, created_by_user_name AS CreatedByUserName
            FROM asset_assignment_notes WHERE asset_assignment_id = @AssignmentId ORDER BY created_at DESC";
        return conn.Query<AssetAssignmentNote>(sql, new { AssignmentId = assignmentId }).ToList();
    }
}
