using AccessManager.UI.Services.Git;

namespace AccessManager.CodeModification.Tests;

internal sealed class StubGitService : IGitService
{
    public Task<GitResult> CommitAndPushAsync(
        IReadOnlyList<string> relativePaths,
        string commitMessage,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(GitResult.Ok());

    public Task<GitResult> CreateBranchAndPushAsync(
        string branchName,
        IReadOnlyList<string> relativePaths,
        string commitMessage,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(GitResult.Ok());

    public Task<GitResult> CreateGitHubPullRequestAsync(
        string branchName,
        string title,
        string? body,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(GitResult.Ok("https://github.com/example/pr/1"));
}
