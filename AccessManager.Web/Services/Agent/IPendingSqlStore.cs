namespace AccessManager.UI.Services.Agent;

/// <summary>Konuşma bazında onay bekleyen normalize edilmiş SELECT SQL.</summary>
public interface IPendingSqlStore
{
    void Set(int conversationId, string normalizedSql);
    string? Get(int conversationId);
    void Clear(int conversationId);
}
