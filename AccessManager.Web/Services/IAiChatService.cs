namespace AccessManager.UI.Services;

public interface IAiChatService
{
    /// <summary>
    /// Kullanıcı mesajını kod bağlamı ile OpenAI'a gönderir, cevabı döner.
    /// </summary>
    Task<string> SendAsync(string userMessage, CancellationToken cancellationToken = default);
}
