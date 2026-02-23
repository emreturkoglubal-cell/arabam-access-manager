namespace AccessManager.UI.Services.Agent;

/// <summary>
/// AI agent'ın kullandığı araçları çalıştırır: read_file, write_file, apply_diff, git_commit_and_push, confirm_and_push.
/// </summary>
public interface IAgentTools
{
    string ReadFile(string relativePath);
    Task<string> WriteFileAsync(string relativePath, string content, int? conversationId = null, CancellationToken cancellationToken = default);
    Task<string> ApplyDiffAsync(string relativePath, string unifiedDiff, int? conversationId = null, CancellationToken cancellationToken = default);
    Task<string> GitCommitAndPushAsync(string commitMessage, IReadOnlyList<string> relativePaths, CancellationToken cancellationToken = default);
    /// <summary>Onay bekleyen değişiklikleri (PendingPushStore) commit edip push eder.</summary>
    Task<string> ConfirmPushAsync(int conversationId, CancellationToken cancellationToken = default);
}
