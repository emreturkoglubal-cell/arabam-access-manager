namespace AccessManager.UI.Services.Agent;

/// <summary>
/// AI agent'ın kullandığı araçları çalıştırır: read_file, write_file, apply_diff, git_commit_and_push.
/// </summary>
public interface IAgentTools
{
    string ReadFile(string relativePath);
    string WriteFile(string relativePath, string content);
    Task<string> ApplyDiffAsync(string relativePath, string unifiedDiff, CancellationToken cancellationToken = default);
    Task<string> GitCommitAndPushAsync(string commitMessage, IReadOnlyList<string> relativePaths, CancellationToken cancellationToken = default);
}
