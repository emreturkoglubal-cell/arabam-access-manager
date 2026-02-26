using AccessManager.Domain.Entities;

namespace AccessManager.Infrastructure.Repositories;

public interface IAiConversationRepository
{
    int CreateConversation(int userId, string title);
    void UpdateConversationUpdatedAt(int conversationId);
    void AddMessage(int conversationId, string role, string content);
    IReadOnlyList<AiConversation> GetConversationsByUser(int userId);
    (IReadOnlyList<AiConversation> Items, int Total) GetConversationsByUserPaged(int userId, int skip, int take);
    IReadOnlyList<AiConversationMessage> GetMessagesByConversation(int conversationId);
    AiConversation? GetConversation(int conversationId, int userId);
    /// <summary>Konuşmayı soft delete yapar (is_active = false). Dönen değer güncellenen satır sayısı (1 veya 0).</summary>
    bool SetConversationInactive(int conversationId, int userId);
}
