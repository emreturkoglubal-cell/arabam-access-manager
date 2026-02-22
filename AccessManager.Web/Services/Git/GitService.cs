using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace AccessManager.UI.Services.Git;

public sealed class GitService : IGitService
{
    private readonly IConfiguration _config;
    private readonly ILogger<GitService> _logger;

    public GitService(IConfiguration config, ILogger<GitService> logger)
    {
        _config = config;
        _logger = logger;
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

        // Normalize paths for comparison (forward slash, no leading slash)
        var normalizedPaths = relativePaths
            .Select(p => p.Replace('\\', '/').TrimStart('/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // git add -f: force add (ignore ignore rules); sadece verilen path'leri stage'le
        var addArgs = "add -f -- " + string.Join(" ", relativePaths.Select(p => $"\"{p.Replace("\"", "\\\"")}\""));
        var addResult = await RunGitAsync(repo, addArgs, cancellationToken);
        if (!addResult.Success)
        {
            _logger.LogError(
                "GitService.CommitAndPush: git add başarısız. RepoPath: {RepoPath}, Paths: {Paths}, AddOutput: {AddOutput}",
                repo, string.Join("; ", relativePaths), addResult.Output ?? "(boş)");
            return GitResult.Fail("git add hatası: " + addResult.Output);
        }

        // Stage'de gerçekten bir şey var mı kontrol et (container'da "no changes added" önlemek için)
        var diffCached = await RunGitAsync(repo, "diff --cached --name-only", cancellationToken);
        var staged = diffCached.Success && !string.IsNullOrWhiteSpace(diffCached.Output)
            ? diffCached.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim().Replace('\\', '/').TrimStart('/'))
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var anyStaged = normalizedPaths.Any(p => staged.Contains(p) || staged.Any(s => s.EndsWith(p, StringComparison.OrdinalIgnoreCase)));
        if (!anyStaged)
        {
            var statusResult = await RunGitAsync(repo, "status --short", cancellationToken);
            var statusPreview = statusResult.Success && !string.IsNullOrEmpty(statusResult.Output)
                ? (statusResult.Output.Length > 1500 ? statusResult.Output.AsSpan(0, 1500).ToString() + "..." : statusResult.Output)
                : "(git status alınamadı)";
            _logger.LogError(
                "GitService.CommitAndPush: Hiçbir path stage'lenemedi. RepoPath: {RepoPath}, İstenenPaths: {Paths}, Stagedekiler: {Staged}, GitStatus: {Status}",
                repo, string.Join("; ", normalizedPaths), string.Join("; ", staged), statusPreview);
            return GitResult.Fail(
                "Değişiklikler stage'lenemedi (git add sonrası dosya yok). " +
                "Container'da repo kopyası eksik/bozuk olabilir (silinmiş dosyalar, farklı path). " +
                "Dockerfile'da tüm kaynak ve .git kopyalandığından emin olun; Git:RepoPath doğru olmalı.");
        }

        // git commit
        var safeMessage = commitMessage.Replace("\"", "\\\"");
        var commitArgs = $"-c user.name=\"{userName}\" -c user.email=\"{userEmail}\" commit -m \"{safeMessage}\"";
        var commitResult = await RunGitAsync(repo, commitArgs, cancellationToken);
        var nothingToCommit = commitResult.Output != null &&
            (commitResult.Output.Contains("nothing to commit", StringComparison.OrdinalIgnoreCase) ||
             commitResult.Output.Contains("working tree clean", StringComparison.OrdinalIgnoreCase));
        if (!commitResult.Success && !nothingToCommit)
        {
            var msg = commitResult.Output ?? "";
            var statusResult = await RunGitAsync(repo, "status --short", cancellationToken);
            var statusPreview = statusResult.Success && !string.IsNullOrEmpty(statusResult.Output)
                ? (statusResult.Output.Length > 1500 ? statusResult.Output.AsSpan(0, 1500).ToString() + "..." : statusResult.Output)
                : "(yok)";
            _logger.LogError(
                "GitService.CommitAndPush: git commit başarısız. RepoPath: {RepoPath}, Paths: {Paths}, CommitOutput: {CommitOutput}, GitStatus: {Status}",
                repo, string.Join("; ", relativePaths), msg, statusPreview);
            if (msg.Contains("no changes added to commit", StringComparison.OrdinalIgnoreCase))
                return GitResult.Fail(
                    "git commit: Hiçbir değişiklik stage'lenmedi. " +
                    "Repo'da birçok dosya 'deleted' görünüyorsa Docker image eksik kopyalanmış olabilir. " +
                    "Build'de COPY . . ile tüm kaynak (.git, .gitignore, docs dahil) kopyalanmalı. Detay: " + msg);
            return GitResult.Fail("git commit hatası: " + msg);
        }

        // Mevcut branch adını al (main/master vb. repoya göre değişir)
        var branch = await GetCurrentBranchAsync(repo, cancellationToken);
        if (string.IsNullOrWhiteSpace(branch))
            branch = "main";

        var remoteUrl = await GetRemoteOriginUrlAsync(repo, cancellationToken);
        if (string.IsNullOrEmpty(remoteUrl))
            return GitResult.Fail("Remote origin URL alınamadı.");

        var authUrl = !string.IsNullOrEmpty(token) ? InjectTokenIntoUrl(remoteUrl, token) : null;
        var pushTarget = authUrl ?? "origin";

        // Canlıda "rejected (fetch first)" önlemek: push öncesi remote'daki değişiklikleri alıp rebase ile üste koy
        var pullArgs = $"pull --rebase \"{pushTarget}\" {branch}";
        var pullResult = await RunGitWithEnvAsync(repo, pullArgs,
            new Dictionary<string, string> { ["GIT_TERMINAL_PROMPT"] = "0" }, cancellationToken);
        if (!pullResult.Success)
        {
            var pullOut = pullResult.Output ?? "";
            _logger.LogError(
                "GitService.CommitAndPush: pull --rebase başarısız. RepoPath: {RepoPath}, Branch: {Branch}, PullOutput: {Output}",
                repo, branch, pullOut);
            return GitResult.Fail("Push öncesi pull başarısız (remote ile birleştirilemedi veya conflict). Hata: " + pullOut);
        }

        // git push
        var pushResult = await RunGitWithEnvAsync(repo, "push \"" + pushTarget + "\" " + branch,
            new Dictionary<string, string> { ["GIT_TERMINAL_PROMPT"] = "0" }, cancellationToken);
        if (!pushResult.Success)
        {
            _logger.LogError(
                "GitService.CommitAndPush: push başarısız. RepoPath: {RepoPath}, Branch: {Branch}, PushOutput: {Output}",
                repo, branch, pushResult.Output ?? "(boş)");
            return GitResult.Fail("Commit atıldı; push başarısız. Branch: " + branch + ". Hata: " + (pushResult.Output ?? "?"));
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

    /// <summary>Repodaki mevcut branch adını döner (main, master vb.).</summary>
    private static async Task<string?> GetCurrentBranchAsync(string repoPath, CancellationToken ct)
    {
        var r = await RunGitAsync(repoPath, "branch --show-current", ct);
        var name = r.Success ? r.Output?.Trim() : null;
        if (!string.IsNullOrWhiteSpace(name)) return name;
        var rev = await RunGitAsync(repoPath, "rev-parse --abbrev-ref HEAD", ct);
        return rev.Success ? rev.Output?.Trim() : null;
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
