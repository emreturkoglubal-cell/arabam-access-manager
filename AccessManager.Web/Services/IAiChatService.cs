namespace AccessManager.UI.Services;

public interface IAiChatService
{
    /// <summary>
    /// Kullanıcı mesajını kod bağlamı ile OpenAI'a gönderir, cevabı döner.
    /// Önceki mesajlar verilirse konuşma bağlamı olarak eklenir.
    /// </summary>
    Task<string> SendAsync(string userMessage, IReadOnlyList<(string Role, string Content)>? previousMessages = null, CancellationToken cancellationToken = default);
}
