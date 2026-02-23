namespace AccessManager.UI.Services;

public interface IAiChatService
{
    /// <summary>
    /// Kullanıcı mesajını kod bağlamı ile OpenAI'a gönderir, cevabı döner.
    /// conversationId: onay bekleyen push'ları eşleştirmek için (apply_diff/write_file sonrası confirm_and_push).
    /// </summary>
    Task<string> SendAsync(string userMessage, IReadOnlyList<(string Role, string Content)>? previousMessages = null, int? conversationId = null, CancellationToken cancellationToken = default);
}
