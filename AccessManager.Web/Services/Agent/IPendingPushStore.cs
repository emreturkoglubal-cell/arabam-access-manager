namespace AccessManager.UI.Services.Agent;

/// <summary>
/// Konuşma bazında "onay bekleyen push" bilgisini tutar. apply_diff/write_file sonrası commit atılmaz;
/// kullanıcı onayladığında confirm_and_push bu kaydı kullanır.
/// </summary>
public interface IPendingPushStore
{
    void Set(int conversationId, IReadOnlyList<string> paths, string commitMessage, string? diffOrSummary = null);
    (IReadOnlyList<string> Paths, string CommitMessage, string? DiffOrSummary)? Get(int conversationId);
    void Clear(int conversationId);
}
