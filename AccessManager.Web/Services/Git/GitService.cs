using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AccessManager.UI.Services.Git;

public sealed class GitService : IGitService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GitService> _logger;

    public GitService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<GitService> logger)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
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

    /// <summary>Base branch her zaman main (Cloud Run main'e push'ta otomatik deploy eder).</summary>
    private const string MainBranch = "main";

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

        var env = new Dictionary<string, string> { ["GIT_TERMINAL_PROMPT"] = "0" };

        // 1) Rebase/merge kalıntısı varsa temizle (canlıda "rebase-merge directory" hatası önlenir)
        var rebaseMerge = Path.Combine(repo, ".git", "rebase-merge");
        var rebaseApply = Path.Combine(repo, ".git", "rebase-apply");
        if (Directory.Exists(rebaseMerge) || Directory.Exists(rebaseApply))
        {
            var abortResult = await RunGitWithEnvAsync(repo, "rebase --abort", env, cancellationToken);
            if (!abortResult.Success)
                _logger.LogError("GitService.CommitAndPush: rebase --abort tamamlanamadı. Output: {Output}", abortResult.Output ?? "(boş)");
        }

        // 2) Her zaman main branch üzerinde çalış
        var currentBranch = await GetCurrentBranchAsync(repo, cancellationToken);
        if (!string.Equals(currentBranch, MainBranch, StringComparison.OrdinalIgnoreCase))
        {
            var coResult = await RunGitWithEnvAsync(repo, "checkout " + MainBranch, env, cancellationToken);
            if (!coResult.Success)
            {
                // main yoksa origin/main'dan oluştur
                var originUrl = await GetRemoteOriginUrlAsync(repo, cancellationToken);
                var fetchAuthUrl = !string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(originUrl) ? InjectTokenIntoUrl(originUrl, token) : null;
                var fetchTarget = fetchAuthUrl ?? "origin";
                var fetchResult = await RunGitWithEnvAsync(repo, "fetch \"" + fetchTarget + "\" " + MainBranch, env, cancellationToken);
                if (!fetchResult.Success)
                {
                    _logger.LogError("GitService.CommitAndPush: fetch origin main başarısız. Output: {Output}", fetchResult.Output ?? "(boş)");
                    return GitResult.Fail("main branch alınamadı. Hata: " + (fetchResult.Output ?? "?"));
                }
                // URL ile fetch yapınca ref FETCH_HEAD'de olur; origin/main güncellenmez. Bu yüzden FETCH_HEAD'den branch oluştur.
                var createResult = await RunGitWithEnvAsync(repo, "checkout -b " + MainBranch + " FETCH_HEAD", env, cancellationToken);
                if (!createResult.Success)
                {
                    _logger.LogError("GitService.CommitAndPush: checkout -b main FETCH_HEAD başarısız. Output: {Output}", createResult.Output ?? "(boş)");
                    return GitResult.Fail("main branch'a geçilemedi. Hata: " + (createResult.Output ?? "?"));
                }
            }
        }

        // Normalize paths for comparison (forward slash, no leading slash)
        var normalizedPaths = relativePaths
            .Select(p => p.Replace('\\', '/').TrimStart('/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 3) git add -f: sadece verilen path'leri stage'le
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

        // 4) Remote: her zaman main'e push (Cloud Run main'de otomatik deploy)
        var remoteUrl = await GetRemoteOriginUrlAsync(repo, cancellationToken);
        if (string.IsNullOrEmpty(remoteUrl))
            return GitResult.Fail("Remote origin URL alınamadı.");

        var authUrl = !string.IsNullOrEmpty(token) ? InjectTokenIntoUrl(remoteUrl, token) : null;
        var pushTarget = authUrl ?? "origin";

        // 5) Push öncesi origin/main'ı çekip kendi commit'imizi üste koy
        var pullArgs = $"pull --rebase \"{pushTarget}\" {MainBranch}";
        var pullResult = await RunGitWithEnvAsync(repo, pullArgs, env, cancellationToken);
        if (!pullResult.Success)
        {
            var pullOut = pullResult.Output ?? "";
            _logger.LogError(
                "GitService.CommitAndPush: pull --rebase origin main başarısız. RepoPath: {RepoPath}, PullOutput: {Output}",
                repo, pullOut);
            return GitResult.Fail("Push öncesi pull (origin main) başarısız. Hata: " + pullOut);
        }

        // 6) Her zaman main branch'e push
        var pushResult = await RunGitWithEnvAsync(repo, "push \"" + pushTarget + "\" " + MainBranch, env, cancellationToken);
        if (!pushResult.Success)
        {
            _logger.LogError(
                "GitService.CommitAndPush: push origin main başarısız. RepoPath: {RepoPath}, PushOutput: {Output}",
                repo, pushResult.Output ?? "(boş)");
            return GitResult.Fail("Push (origin main) başarısız. Hata: " + (pushResult.Output ?? "?"));
        }

        return GitResult.Ok();
    }

    public async Task<GitResult> CreateBranchAndPushAsync(
        string branchName,
        IReadOnlyList<string> relativePaths,
        string commitMessage,
        CancellationToken cancellationToken = default)
    {
        if (relativePaths.Count == 0)
            return GitResult.Fail("Commit için en az bir dosya gerekli.");
        if (string.IsNullOrWhiteSpace(branchName) || branchName.Contains("..") || branchName.Length > 200)
            return GitResult.Fail("Geçersiz branch adı.");

        var repo = RepoPath;
        var token = _config["Git:Token"]?.Trim();
        var userName = _config["Git:UserName"]?.Trim() ?? "Access Manager";
        var userEmail = _config["Git:UserEmail"]?.Trim() ?? "ai@local";
        var env = new Dictionary<string, string> { ["GIT_TERMINAL_PROMPT"] = "0" };

        foreach (var rel in relativePaths)
        {
            if (string.IsNullOrWhiteSpace(rel) || rel.Contains(".."))
                return GitResult.Fail($"Geçersiz dosya yolu: {rel}");
        }

        var currentBranch = await GetCurrentBranchAsync(repo, cancellationToken);
        if (!string.Equals(currentBranch, MainBranch, StringComparison.OrdinalIgnoreCase))
        {
            var coMain = await RunGitWithEnvAsync(repo, "checkout " + MainBranch, env, cancellationToken);
            if (!coMain.Success)
                return GitResult.Fail("main branch'a geçilemedi: " + (coMain.Output ?? "?"));
        }

        var addArgs = "add -f -- " + string.Join(" ", relativePaths.Select(p => $"\"{p.Replace("\"", "\\\"")}\""));
        var safeMessage = commitMessage.Replace("\"", "\\\"");
        var commitArgs = $"-c user.name=\"{userName}\" -c user.email=\"{userEmail}\" commit -m \"{safeMessage}\"";

        var coResult = await RunGitWithEnvAsync(repo, "checkout -b \"" + branchName.Replace("\"", "\\\"") + "\"", env, cancellationToken);
        if (!coResult.Success)
            return GitResult.Fail("Branch oluşturulamadı: " + (coResult.Output ?? "?"));

        var addResult = await RunGitAsync(repo, addArgs, cancellationToken);
        if (!addResult.Success)
            return GitResult.Fail("git add hatası: " + addResult.Output);

        var commitResult = await RunGitAsync(repo, commitArgs, cancellationToken);
        var nothingToCommit = commitResult.Output != null &&
            (commitResult.Output.Contains("nothing to commit", StringComparison.OrdinalIgnoreCase) ||
             commitResult.Output.Contains("working tree clean", StringComparison.OrdinalIgnoreCase));
        if (!commitResult.Success && !nothingToCommit)
            return GitResult.Fail("git commit hatası: " + (commitResult.Output ?? "?"));

        var remoteUrl = await GetRemoteOriginUrlAsync(repo, cancellationToken);
        if (string.IsNullOrEmpty(remoteUrl))
            return GitResult.Fail("Remote origin URL alınamadı.");
        var authUrl = !string.IsNullOrEmpty(token) ? InjectTokenIntoUrl(remoteUrl, token) : null;
        var pushTarget = authUrl ?? "origin";

        var pushResult = await RunGitWithEnvAsync(repo, "push \"" + pushTarget + "\" " + branchName, env, cancellationToken);
        if (!pushResult.Success)
            return GitResult.Fail("Branch push başarısız: " + (pushResult.Output ?? "?"));

        var branchNow = await GetCurrentBranchAsync(repo, cancellationToken);
        if (!string.Equals(branchNow, MainBranch, StringComparison.OrdinalIgnoreCase))
        {
            var backToMain = await RunGitWithEnvAsync(repo, "checkout " + MainBranch, env, cancellationToken);
            if (!backToMain.Success)
                _logger.LogWarning("CreateBranchAndPush: main'e geri dönülemedi. Output: {Output}", backToMain.Output);
        }

        return GitResult.Ok($"Branch '{branchName}' oluşturuldu ve pushlandı. GitHub/GitLab'da 'Compare & pull request' ile PR açabilirsiniz.");
    }

    public async Task<GitResult> CreateGitHubPullRequestAsync(
        string branchName,
        string title,
        string? body,
        CancellationToken cancellationToken = default)
    {
        var repo = RepoPath;
        var remoteUrl = await GetRemoteOriginUrlAsync(repo, cancellationToken);
        if (string.IsNullOrEmpty(remoteUrl))
            return GitResult.Fail("Remote origin URL alınamadı.");

        if (!TryParseGitHubOwnerRepo(remoteUrl, out var owner, out var repoName))
            return GitResult.Fail("Sadece GitHub remote destekleniyor. URL: " + remoteUrl);

        var token = _config["Git:GitHubToken"]?.Trim() ?? _config["Git:Token"]?.Trim();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("CreateGitHubPullRequest: Git:GitHubToken veya Git:Token yok; PR API çağrısı atlanır.");
            return GitResult.Fail("GitHub token yapılandırılmamış (Git:GitHubToken veya Git:Token). PR'ı GitHub'da 'Compare & pull request' ile açabilirsiniz.");
        }

        using var http = _httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        http.DefaultRequestHeaders.Add("User-Agent", "Arabam-AccessManager");
        http.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

        var payload = new { title, head = branchName, @base = MainBranch, body = body ?? "" };
        var response = await http.PostAsJsonAsync(
            $"https://api.github.com/repos/{owner}/{repoName}/pulls",
            payload,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("CreateGitHubPullRequest: API hatası {StatusCode}. Body: {Body}", response.StatusCode, err);
            return GitResult.Fail("GitHub PR oluşturulamadı: " + response.StatusCode + ". " + (err.Length > 200 ? err.AsSpan(0, 200).ToString() + "..." : err));
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var htmlUrl = doc.RootElement.TryGetProperty("html_url", out var urlProp) ? urlProp.GetString() : null;
        if (string.IsNullOrEmpty(htmlUrl))
            return GitResult.Fail("GitHub yanıtında PR linki bulunamadı.");
        return GitResult.Ok(htmlUrl);
    }

    private static bool TryParseGitHubOwnerRepo(string remoteUrl, out string owner, out string repoName)
    {
        owner = "";
        repoName = "";
        if (string.IsNullOrWhiteSpace(remoteUrl)) return false;
        var span = remoteUrl.AsSpan().Trim();
        if (span.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase))
        {
            span = span.Slice("https://github.com/".Length);
        }
        else if (span.StartsWith("http://github.com/", StringComparison.OrdinalIgnoreCase))
        {
            span = span.Slice("http://github.com/".Length);
        }
        else if (span.StartsWith("git@github.com:", StringComparison.OrdinalIgnoreCase))
        {
            span = span.Slice("git@github.com:".Length);
        }
        else
            return false;
        var slash = span.IndexOf('/');
        if (slash < 0) return false;
        owner = span.Slice(0, slash).ToString();
        repoName = span.Slice(slash + 1).ToString().TrimEnd('/');
        if (repoName.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            repoName = repoName.Substring(0, repoName.Length - 4);
        return !string.IsNullOrEmpty(owner) && !string.IsNullOrEmpty(repoName);
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

    /// <summary>Repodaki mevcut branch adını döner (checkout main için kullanılır).</summary>
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
