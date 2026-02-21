namespace AccessManager.UI.Services.Git;

public interface IGitService
{
    /// <summary>
    /// Verilen dosyaları stage'leyip commit atar ve origin main'e push eder.
    /// Repo yolu ve kimlik bilgileri config'den (Git:*) okunur.
    /// </summary>
    Task<GitResult> CommitAndPushAsync(
        IReadOnlyList<string> relativePaths,
        string commitMessage,
        CancellationToken cancellationToken = default);
}
