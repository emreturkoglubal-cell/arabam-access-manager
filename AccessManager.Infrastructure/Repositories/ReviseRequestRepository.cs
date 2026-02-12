using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class ReviseRequestRepository : IReviseRequestRepository
{
    private readonly string _connectionString;

    public ReviseRequestRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IReadOnlyList<ReviseRequest> GetAll()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, title AS Title, description AS Description, status AS Status,
            created_by_user_id AS CreatedByUserId, created_by_user_name AS CreatedByUserName,
            created_at AS CreatedAt, updated_at AS UpdatedAt, resolved_at AS ResolvedAt
            FROM revise_requests ORDER BY created_at DESC";
        return conn.Query<ReviseRequest>(sql).ToList();
    }

    public ReviseRequest? GetById(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, title AS Title, description AS Description, status AS Status,
            created_by_user_id AS CreatedByUserId, created_by_user_name AS CreatedByUserName,
            created_at AS CreatedAt, updated_at AS UpdatedAt, resolved_at AS ResolvedAt
            FROM revise_requests WHERE id = @Id";
        return conn.QuerySingleOrDefault<ReviseRequest>(sql, new { Id = id });
    }

    public ReviseRequest? GetByIdWithImages(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, title AS Title, description AS Description, status AS Status,
            created_by_user_id AS CreatedByUserId, created_by_user_name AS CreatedByUserName,
            created_at AS CreatedAt, updated_at AS UpdatedAt, resolved_at AS ResolvedAt
            FROM revise_requests WHERE id = @Id";
        var request = conn.QuerySingleOrDefault<ReviseRequest>(sql, new { Id = id });
        if (request != null)
        {
            request.Images = GetImages(id).ToList();
        }
        return request;
    }

    public int Insert(ReviseRequest request)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO revise_requests (title, description, status, created_by_user_id, created_by_user_name)
            VALUES (@Title, @Description, @Status, @CreatedByUserId, @CreatedByUserName) RETURNING id";
        return conn.ExecuteScalar<int>(sql, new
        {
            request.Title,
            request.Description,
            Status = (short)request.Status,
            request.CreatedByUserId,
            request.CreatedByUserName
        });
    }

    public void Update(ReviseRequest request)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"UPDATE revise_requests SET title=@Title, description=@Description, status=@Status, updated_at=now(), resolved_at=@ResolvedAt WHERE id=@Id";
        conn.Execute(sql, new
        {
            request.Id,
            request.Title,
            request.Description,
            Status = (short)request.Status,
            request.ResolvedAt
        });
    }

    public void UpdateStatus(int id, ReviseRequestStatus status)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"UPDATE revise_requests SET status=@Status, updated_at=now(), resolved_at=@ResolvedAt WHERE id=@Id";
        conn.Execute(sql, new
        {
            Id = id,
            Status = (short)status,
            ResolvedAt = status == ReviseRequestStatus.Resolved ? DateTime.UtcNow : (DateTime?)null
        });
    }

    public IReadOnlyList<ReviseRequestImage> GetImages(int reviseRequestId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, revise_request_id AS ReviseRequestId, file_name AS FileName, file_path AS FilePath,
            file_size AS FileSize, mime_type AS MimeType, display_order AS DisplayOrder, created_at AS CreatedAt
            FROM revise_request_images WHERE revise_request_id = @ReviseRequestId ORDER BY display_order, id";
        return conn.Query<ReviseRequestImage>(sql, new { ReviseRequestId = reviseRequestId }).ToList();
    }

    public void InsertImage(ReviseRequestImage image)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO revise_request_images (revise_request_id, file_name, file_path, file_size, mime_type, display_order)
            VALUES (@ReviseRequestId, @FileName, @FilePath, @FileSize, @MimeType, @DisplayOrder) RETURNING id";
        var id = conn.ExecuteScalar<int>(sql, new
        {
            image.ReviseRequestId,
            image.FileName,
            image.FilePath,
            image.FileSize,
            image.MimeType,
            image.DisplayOrder
        });
        image.Id = id;
    }

    public ReviseRequestImage? GetImageById(int imageId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, revise_request_id AS ReviseRequestId, file_name AS FileName, file_path AS FilePath,
            file_size AS FileSize, mime_type AS MimeType, display_order AS DisplayOrder, created_at AS CreatedAt
            FROM revise_request_images WHERE id = @Id";
        return conn.QuerySingleOrDefault<ReviseRequestImage>(sql, new { Id = imageId });
    }

    public void DeleteImage(int imageId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        conn.Execute("DELETE FROM revise_request_images WHERE id = @Id", new { Id = imageId });
    }
}
