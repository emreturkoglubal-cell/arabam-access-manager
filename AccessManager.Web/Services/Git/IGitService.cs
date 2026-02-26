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

    /// <summary>
    /// Yeni branch oluşturur, değişiklikleri commit edip bu branch'i remote'a push eder (main'e değil). PR açmak için kullanılır.
    /// </summary>
    Task<GitResult> CreateBranchAndPushAsync(
        string branchName,
        IReadOnlyList<string> relativePaths,
        string commitMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// GitHub API ile pull request oluşturur. Başarıda Message'da PR URL (html_url) döner.
    /// </summary>
    Task<GitResult> CreateGitHubPullRequestAsync(
        string branchName,
        string title,
        string? body,
        CancellationToken cancellationToken = default);
}
