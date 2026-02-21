using System.Diagnostics;
using System.Text;

namespace AccessManager.UI.Services.Git;

public sealed class GitService : IGitService
{
    private readonly IConfiguration _config;

    public GitService(IConfiguration config)
    {
        _config = config;
    }

    private string RepoPath
    {
        get
        {
            var path = _config["Git:RepoPath"]?.Trim();
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException("Git:RepoPath yapılandırması gerekli.");
            path = Path.GetFullPath(path);
            if (!Directory.Exists(Path.Combine(path, ".git")))
                throw new InvalidOperationException($"Git repo bulunamadı: {path}");
            return path;
        }
    }

    public async Task<GitResult> CommitAndPushAsync(
        IReadOnlyList<string> relativePaths,
        string commitMessage,
        CancellationToken cancellationToken = default)
    {
        if (relativePaths.Count == 0)
            return GitResult.Fail("Commit için en az bir dosya gerekli.");

        var repo = RepoPath;
        var token = _config["Git:Token"]?.Trim();
        var userName = _config["Git:UserName"]?.Trim() ?? "Access Manager";
        var userEmail = _config["Git:UserEmail"]?.Trim() ?? "ai@local";

        foreach (var rel in relativePaths)
        {
            if (string.IsNullOrWhiteSpace(rel) || rel.Contains(".."))
                return GitResult.Fail($"Geçersiz dosya yolu: {rel}");
        }

        // git add
        var addArgs = "add -- " + string.Join(" ", relativePaths.Select(p => $"\"{p.Replace("\"", "\\\"")}\""));
        var addResult = await RunGitAsync(repo, addArgs, cancellationToken);
        if (!addResult.Success)
            return GitResult.Fail("git add hatası: " + addResult.Output);

        // git commit
        var safeMessage = commitMessage.Replace("\"", "\\\"");
        var commitArgs = $"-c user.name=\"{userName}\" -c user.email=\"{userEmail}\" commit -m \"{safeMessage}\"";
        var commitResult = await RunGitAsync(repo, commitArgs, cancellationToken);
        if (!commitResult.Success)
            return GitResult.Fail("git commit hatası: " + commitResult.Output);

        // git push origin main (with optional token)
        if (!string.IsNullOrEmpty(token))
        {
            var remoteUrl = await GetRemoteOriginUrlAsync(repo, cancellationToken);
            if (string.IsNullOrEmpty(remoteUrl))
                return GitResult.Fail("Remote origin URL alınamadı.");
            var authUrl = InjectTokenIntoUrl(remoteUrl, token);
            var pushResult = await RunGitWithEnvAsync(repo, "push \"" + authUrl + "\" main",
                new Dictionary<string, string> { ["GIT_TERMINAL_PROMPT"] = "0" }, cancellationToken);
            if (!pushResult.Success)
                return GitResult.Fail("git push hatası: " + pushResult.Output);
        }
        else
        {
            var pushResult = await RunGitAsync(repo, "push origin main", cancellationToken);
            if (!pushResult.Success)
                return GitResult.Fail("git push hatası: " + pushResult.Output);
        }

        return GitResult.Ok();
    }

    private static string InjectTokenIntoUrl(string url, string token)
    {
        if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return "https://" + token + "@" + url.AsSpan(8).ToString();
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            return "http://" + token + "@" + url.AsSpan(7).ToString();
        return url;
    }

    private static async Task<string?> GetRemoteOriginUrlAsync(string repoPath, CancellationToken ct)
    {
        var r = await RunGitAsync(repoPath, "remote get-url origin", ct);
        return r.Success ? r.Output?.Trim() : null;
    }

    private static async Task<(bool Success, string? Output)> RunGitAsync(string repoPath, string arguments, CancellationToken ct)
    {
        return await RunGitWithEnvAsync(repoPath, arguments, null, ct);
    }

    private static async Task<(bool Success, string? Output)> RunGitWithEnvAsync(
        string repoPath,
        string arguments,
        Dictionary<string, string>? env,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = repoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        if (env != null)
        {
            foreach (var kv in env)
                psi.Environment[kv.Key] = kv.Value;
        }

        try
        {
            using var process = Process.Start(psi);
            if (process == null)
                return (false, "Process başlatılamadı.");

            var stdout = await process.StandardOutput.ReadToEndAsync(ct);
            var stderr = await process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);
            var output = (stdout + "\n" + stderr).Trim();
            return (process.ExitCode == 0, output);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
