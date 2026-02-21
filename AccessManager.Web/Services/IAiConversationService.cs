using AccessManager.Domain.Entities;

namespace AccessManager.UI.Services;

public interface IAiConversationService
{
    IReadOnlyList<AiConversation> GetConversationsForCurrentUser();
    AiConversation? GetConversation(int conversationId);
    IReadOnlyList<AiConversationMessage> GetMessages(int conversationId);
    Task<(int ConversationId, string Title, string Reply)> SendMessageAsync(int? conversationId, string userMessage, CancellationToken cancellationToken = default);
}
