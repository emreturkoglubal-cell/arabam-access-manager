using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Infrastructure.Repositories;

namespace AccessManager.UI.Services;

public class AiConversationService : IAiConversationService
{
    private const int TitleMaxLength = 80;
    private readonly ICurrentUserService _currentUser;
    private readonly IAiConversationRepository _repo;
    private readonly IAiChatService _chat;

    public AiConversationService(
        ICurrentUserService currentUser,
        IAiConversationRepository repo,
        IAiChatService chat)
    {
        _currentUser = currentUser;
        _repo = repo;
        _chat = chat;
    }

    public IReadOnlyList<AiConversation> GetConversationsForCurrentUser()
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Array.Empty<AiConversation>();
        return _repo.GetConversationsByUser(userId.Value);
    }

    public AiConversation? GetConversation(int conversationId)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return null;
        return _repo.GetConversation(conversationId, userId.Value);
    }

    public IReadOnlyList<AiConversationMessage> GetMessages(int conversationId)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Array.Empty<AiConversationMessage>();
        var conv = _repo.GetConversation(conversationId, userId.Value);
        if (conv == null) return Array.Empty<AiConversationMessage>();
        return _repo.GetMessagesByConversation(conversationId);
    }

    public async Task<(int ConversationId, string Title, string Reply)> SendMessageAsync(int? conversationId, string userMessage, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? 0;
        if (userId == 0) return (0, "", "Oturum açmanız gerekiyor.");

        int convId;
        string title;

        if (conversationId.HasValue && conversationId.Value > 0)
        {
            var conv = _repo.GetConversation(conversationId.Value, userId);
            if (conv == null) return (0, "", "Bu konuşmaya erişim yetkiniz yok.");
            convId = conv.Id;
            title = conv.Title;
        }
        else
        {
            var titleRaw = userMessage.Trim();
            title = titleRaw.Length <= TitleMaxLength ? titleRaw : titleRaw[..TitleMaxLength].TrimEnd();
            if (string.IsNullOrEmpty(title)) title = "(Yeni sohbet)";
            convId = _repo.CreateConversation(userId, title);
        }

        var previousMessages = _repo.GetMessagesByConversation(convId)
            .Select(m => (m.Role, m.Content))
            .ToList();

        _repo.AddMessage(convId, "user", userMessage.Trim());
        var reply = await _chat.SendAsync(userMessage.Trim(), previousMessages, cancellationToken);
        _repo.AddMessage(convId, "assistant", reply);

        return (convId, title, reply);
    }
}
