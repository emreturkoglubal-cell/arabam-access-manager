using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace AccessManager.UI.Services;

public class AiConversationService : IAiConversationService
{
    private const int TitleMaxLength = 80;
    private readonly ICurrentUserService _currentUser;
    private readonly IAiConversationRepository _repo;
    private readonly IAiChatService _chat;
    private readonly ILogger<AiConversationService> _logger;

    public AiConversationService(
        ICurrentUserService currentUser,
        IAiConversationRepository repo,
        IAiChatService chat,
        ILogger<AiConversationService> logger)
    {
        _currentUser = currentUser;
        _repo = repo;
        _chat = chat;
        _logger = logger;
    }

    public IReadOnlyList<AiConversation> GetConversationsForCurrentUser()
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Array.Empty<AiConversation>();
        return _repo.GetConversationsByUser(userId.Value);
    }

    public (IReadOnlyList<AiConversation> Items, int Total) GetConversationsPaged(int skip, int take)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return (Array.Empty<AiConversation>(), 0);
        return _repo.GetConversationsByUserPaged(userId.Value, skip, take);
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

    public (string? Title, IReadOnlyList<AiConversationMessage> Messages) GetConversationWithMessages(int conversationId)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return (null, Array.Empty<AiConversationMessage>());
        var (conv, messages) = _repo.GetConversationWithMessages(conversationId, userId.Value);
        return (conv?.Title, messages ?? Array.Empty<AiConversationMessage>());
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

        string reply;
        try
        {
            reply = await _chat.SendAsync(userMessage.Trim(), previousMessages, convId, cancellationToken);
        }
        catch (OutOfMemoryException ex)
        {
            _logger.LogError(ex, "AiConversationService.SendMessageAsync: Bellek yetersiz (OOM). ConversationId: {ConversationId}, UserId: {UserId}", convId, userId);
            reply = "Şu an yanıt üretilemedi (bellek sınırı). Lütfen kısa bir mesajla tekrar deneyin veya daha sonra tekrar deneyin.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AiConversationService.SendMessageAsync: AI yanıt alınırken hata. ConversationId: {ConversationId}, UserId: {UserId}", convId, userId);
            reply = "Yanıt alınırken bir hata oluştu: " + ex.Message;
        }

        _repo.AddMessage(convId, "assistant", reply);
        return (convId, title, reply);
    }

    public async Task<(int ConversationId, string Title, string Reply)> SendMessageStreamAsync(
        int? conversationId,
        string userMessage,
        Func<AiStreamEvent, CancellationToken, ValueTask> emit,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? 0;
        if (userId == 0)
        {
            const string msg = "Oturum açmanız gerekiyor.";
            await emit(new AiStreamEvent { Type = "error", Message = msg }, cancellationToken).ConfigureAwait(false);
            await emit(new AiStreamEvent { Type = "done", ConversationId = 0, Title = "", Reply = msg }, cancellationToken).ConfigureAwait(false);
            return (0, "", msg);
        }

        int convId;
        string title;

        if (conversationId.HasValue && conversationId.Value > 0)
        {
            var conv = _repo.GetConversation(conversationId.Value, userId);
            if (conv == null)
            {
                const string msg = "Bu konuşmaya erişim yetkiniz yok.";
                await emit(new AiStreamEvent { Type = "error", Message = msg }, cancellationToken).ConfigureAwait(false);
                await emit(new AiStreamEvent { Type = "done", ConversationId = 0, Title = "", Reply = msg }, cancellationToken).ConfigureAwait(false);
                return (0, "", msg);
            }
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

        string reply;
        try
        {
            reply = await _chat.SendAsync(userMessage.Trim(), previousMessages, convId, cancellationToken, emit).ConfigureAwait(false);
        }
        catch (OutOfMemoryException ex)
        {
            _logger.LogError(ex, "AiConversationService.SendMessageStreamAsync: Bellek yetersiz (OOM). ConversationId: {ConversationId}, UserId: {UserId}", convId, userId);
            reply = "Şu an yanıt üretilemedi (bellek sınırı). Lütfen kısa bir mesajla tekrar deneyin veya daha sonra tekrar deneyin.";
            await emit(new AiStreamEvent { Type = "error", Message = reply }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AiConversationService.SendMessageStreamAsync: AI yanıt alınırken hata. ConversationId: {ConversationId}, UserId: {UserId}", convId, userId);
            reply = "Yanıt alınırken bir hata oluştu: " + ex.Message;
            await emit(new AiStreamEvent { Type = "error", Message = reply }, cancellationToken).ConfigureAwait(false);
        }

        _repo.AddMessage(convId, "assistant", reply);
        await emit(new AiStreamEvent { Type = "done", ConversationId = convId, Title = title, Reply = reply }, cancellationToken).ConfigureAwait(false);
        return (convId, title, reply);
    }

    public bool DeleteConversation(int conversationId)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return false;
        return _repo.SetConversationInactive(conversationId, userId.Value);
    }
}
