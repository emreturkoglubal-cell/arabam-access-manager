using AccessManager.Domain.Entities;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class AiConversationRepository : IAiConversationRepository
{
    private readonly string _connectionString;

    public AiConversationRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public int CreateConversation(int userId, string title)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO ai_conversations (user_id, title, created_at, updated_at)
            VALUES (@UserId, @Title, now(), now()) RETURNING id";
        return conn.ExecuteScalar<int>(sql, new { UserId = userId, Title = title });
    }

    public void UpdateConversationUpdatedAt(int conversationId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        conn.Execute("UPDATE ai_conversations SET updated_at = now() WHERE id = @Id", new { Id = conversationId });
    }

    public void AddMessage(int conversationId, string role, string content)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        conn.Execute(
            "INSERT INTO ai_conversation_messages (conversation_id, role, content) VALUES (@ConversationId, @Role, @Content)",
            new { ConversationId = conversationId, Role = role, Content = content });
        UpdateConversationUpdatedAt(conversationId);
    }

    public IReadOnlyList<AiConversation> GetConversationsByUser(int userId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, user_id AS UserId, title AS Title, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM ai_conversations WHERE user_id = @UserId ORDER BY updated_at DESC";
        return conn.Query<AiConversation>(sql, new { UserId = userId }).ToList();
    }

    public (IReadOnlyList<AiConversation> Items, int Total) GetConversationsByUserPaged(int userId, int skip, int take)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        var total = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM ai_conversations WHERE user_id = @UserId", new { UserId = userId });
        const string sql = @"SELECT id AS Id, user_id AS UserId, title AS Title, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM ai_conversations WHERE user_id = @UserId ORDER BY updated_at DESC LIMIT @Take OFFSET @Skip";
        var items = conn.Query<AiConversation>(sql, new { UserId = userId, Skip = skip, Take = take }).ToList();
        return (items, total);
    }

    public IReadOnlyList<AiConversationMessage> GetMessagesByConversation(int conversationId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, conversation_id AS ConversationId, role AS Role, content AS Content, created_at AS CreatedAt
            FROM ai_conversation_messages WHERE conversation_id = @ConversationId ORDER BY created_at ASC";
        return conn.Query<AiConversationMessage>(sql, new { ConversationId = conversationId }).ToList();
    }

    public AiConversation? GetConversation(int conversationId, int userId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, user_id AS UserId, title AS Title, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM ai_conversations WHERE id = @Id AND user_id = @UserId";
        return conn.QueryFirstOrDefault<AiConversation>(sql, new { Id = conversationId, UserId = userId });
    }
}
