using AccessManager.Domain.Entities;

namespace AccessManager.UI.Services;

public interface IAiConversationService
{
    IReadOnlyList<AiConversation> GetConversationsForCurrentUser();
    (IReadOnlyList<AiConversation> Items, int Total) GetConversationsPaged(int skip, int take);
    AiConversation? GetConversation(int conversationId);
    IReadOnlyList<AiConversationMessage> GetMessages(int conversationId);
    /// <summary>Tek round-trip ile konuşma başlığı + mesajlar (yetkisizse null).</summary>
    (string? Title, IReadOnlyList<AiConversationMessage> Messages) GetConversationWithMessages(int conversationId);
    Task<(int ConversationId, string Title, string Reply)> SendMessageAsync(int? conversationId, string userMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcı mesajını kaydeder, AI yanıtı üretirken <paramref name="emit"/> ile NDJSON olayları gönderir; bittiğinde done olayı ve asistan mesajı DB'ye yazılır.
    /// </summary>
    Task<(int ConversationId, string Title, string Reply)> SendMessageStreamAsync(
        int? conversationId,
        string userMessage,
        Func<AiStreamEvent, CancellationToken, ValueTask> emit,
        CancellationToken cancellationToken = default);
    /// <summary>Sohbeti soft delete yapar (is_active = false). Sadece kendi sohbeti için true döner.</summary>
    bool DeleteConversation(int conversationId);
}
