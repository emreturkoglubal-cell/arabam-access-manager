using AccessManager.Domain.Entities;

namespace AccessManager.Infrastructure.Repositories;

public interface IAiConversationRepository
{
    int CreateConversation(int userId, string title);
    void UpdateConversationUpdatedAt(int conversationId);
    void AddMessage(int conversationId, string role, string content);
    IReadOnlyList<AiConversation> GetConversationsByUser(int userId);
    IReadOnlyList<AiConversationMessage> GetMessagesByConversation(int conversationId);
    AiConversation? GetConversation(int conversationId, int userId);
}
