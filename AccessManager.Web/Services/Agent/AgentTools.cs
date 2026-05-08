using System.Diagnostics;
using System.Text;
using AccessManager.Application.Interfaces;
using AccessManager.Application.Sql;
using AccessManager.UI.Services.CodeModification;
using AccessManager.UI.Services.Git;

namespace AccessManager.UI.Services.Agent;

public sealed class AgentTools : IAgentTools
{
    private const string BuildSkippedPrefix = "BUILD_ATLANDI:";
    private readonly IConfiguration _config;
    private readonly ICodeModificationService _codeMod;
    private readonly IGitService _gitService;
    private readonly IPendingPushStore _pendingPush;
    private readonly IPendingSqlStore _pendingSql;
    private readonly IReadOnlySqlQueryService _readOnlySql;
    private readonly ILogger<AgentTools> _logger;

    public AgentTools(
        IConfiguration config,
        ICodeModificationService codeMod,
        IGitService gitService,
        IPendingPushStore pendingPush,
        IPendingSqlStore pendingSql,
        IReadOnlySqlQueryService readOnlySql,
        ILogger<AgentTools> logger)
    {
        _config = config;
        _codeMod = codeMod;
        _gitService = gitService;
        _pendingPush = pendingPush;
        _pendingSql = pendingSql;
        _readOnlySql = readOnlySql;
        _logger = logger;
    }

    private string RepoPath
    {
        get
        {
            var path = _config["Git:RepoPath"]?.Trim();
            if (string.IsNullOrEmpty(path))
                return string.Empty;
            return Path.GetFullPath(path);
        }
    }

    private bool TryResolvePath(string relativePath, out string fullPath, out string error)
    {
        error = string.Empty;
        var repo = RepoPath;
        if (string.IsNullOrEmpty(repo))
        {
            error = "Git:RepoPath yapılandırılmamış.";
            fullPath = string.Empty;
            return false;
        }
        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        if (normalized.Contains(".."))
        {
            error = "Geçersiz yol (.. kullanılamaz).";
            fullPath = string.Empty;
            return false;
        }
        fullPath = Path.GetFullPath(Path.Combine(repo, normalized));
        if (!fullPath.StartsWith(repo, StringComparison.OrdinalIgnoreCase))
        {
            error = "Dosya repo dışında.";
            return false;
        }
        return true;
    }

    public string ReadFile(string relativePath)
    {
        var repoPath = RepoPath;
        if (!TryResolvePath(relativePath, out var fullPath, out var error))
        {
            _logger.LogError("AgentTools.ReadFile: Path çözülemedi. Git RepoPath: {RepoPath}, RelativePath: {RelativePath}, Error: {Error}", repoPath ?? "(boş)", relativePath, error);
            return "HATA: " + error;
        }
        _logger.LogError("AgentTools.ReadFile: Git RepoPath: {RepoPath}, RelativePath: {RelativePath}, ResolvedFullPath: {ResolvedFullPath}", repoPath ?? "(boş)", relativePath, fullPath);
        try
        {
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("AgentTools.ReadFile: Dosya yok. RelativePath: {RelativePath}, ResolvedPath: {ResolvedPath}", relativePath, fullPath);
                return "HATA: Dosya bulunamadı: " + relativePath + ". (Canlı ortamda Git:RepoPath altında kaynak kod olmayabilir; container'da sadece derlenmiş uygulama var.)";
            }
            return File.ReadAllText(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AgentTools.ReadFile: Okuma hatası. RelativePath: {RelativePath}, ResolvedPath: {ResolvedPath}", relativePath, fullPath);
            return "HATA: " + ex.Message;
        }
    }

    public async Task<string> WriteFileAsync(string relativePath, string content, int? conversationId = null, CancellationToken cancellationToken = default)
    {
        if (!TryResolvePath(relativePath, out var fullPath, out var error))
            return "HATA: " + error;
        try
        {
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(fullPath, content ?? string.Empty, cancellationToken);
            var pathForGit = relativePath.Replace("\\", "/");
            if (conversationId.HasValue && conversationId.Value > 0)
            {
                var summary = (content ?? "").Length > 2000 ? (content ?? "")[..2000] + "\n... (kesildi)" : (content ?? "");
                _pendingPush.Set(conversationId.Value, new[] { pathForGit }, "Kod güncellemesi (arabam AI)", summary);
            }
            return "OK: Dosya yazıldı. Değiştirilen dosya: " + pathForGit + ". Kullanıcıya yapılan değişikliği (kodu) kod bloğunda gösterip onay iste. Onay gelince confirm_and_push çağır. ASLA bu yanıtta git_commit_and_push çağırma.";
        }
        catch (Exception ex)
        {
            return "HATA: " + ex.Message;
        }
    }

    public async Task<string> ApplyDiffAsync(string relativePath, string unifiedDiff, int? conversationId = null, CancellationToken cancellationToken = default)
    {
        var result = await _codeMod.ApplyDiffAsync(relativePath, unifiedDiff, cancellationToken);
        if (!result.Success)
        {
            _logger.LogError("AgentTools.ApplyDiffAsync: Diff uygulanamadı. Path: {Path}, Hata: {Message}", relativePath, result.Message);
            var fallback = await TrySimpleLineReplaceFallbackAsync(relativePath, unifiedDiff, cancellationToken).ConfigureAwait(false);
            if (fallback.Success)
            {
                var fbPath = fallback.ResolvedPath.Replace("\\", "/");
                if (conversationId.HasValue && conversationId.Value > 0)
                {
                    var diffStored = unifiedDiff.Length > 8000 ? unifiedDiff[..8000] + "\n... (kesildi)" : unifiedDiff;
                    _pendingPush.Set(conversationId.Value, new[] { fbPath }, "Kod güncellemesi (arabam AI)", diffStored);
                }
                return "OK: Diff bağlamı tutmadığı için satır-bazlı güvenli fallback ile değişiklik uygulandı. Değiştirilen dosya: " + fbPath +
                       ". Kullanıcıya değişikliği gösterip onay iste. Onay gelince confirm_and_push çağır.";
            }

            return "HATA: " + result.Message +
                   "\nİpucu (asistan için): Hemen aynı turda read_file ile dosyanın güncel halini tekrar oku; bağlam satırlarını birebir kopyalayarak daha küçük bir apply_diff dene. Gerekirse son çare write_file kullan.";
        }

        var pathForGit = (result.ResolvedPath ?? relativePath).Replace("\\", "/");
        if (conversationId.HasValue && conversationId.Value > 0)
        {
            var diffStored = unifiedDiff.Length > 8000 ? unifiedDiff[..8000] + "\n... (kesildi)" : unifiedDiff;
            _pendingPush.Set(conversationId.Value, new[] { pathForGit }, "Kod güncellemesi (arabam AI)", diffStored);
        }
        return "OK: Diff uygulandı. Değiştirilen dosya: " + pathForGit + ". Kullanıcıya aşağıdaki diff'i kod bloğunda gösterip onay iste. Onay gelince confirm_and_push çağır. ASLA bu yanıtta git_commit_and_push çağırma.\n\nDiff:\n```diff\n" + (unifiedDiff.Length > 8000 ? unifiedDiff[..8000] + "\n... (kesildi)" : unifiedDiff) + "\n```";
    }

    private async Task<(bool Success, string ResolvedPath)> TrySimpleLineReplaceFallbackAsync(string relativePath, string unifiedDiff, CancellationToken cancellationToken)
    {
        if (!TryResolvePath(relativePath, out var fullPath, out _))
            return (false, relativePath);
        if (!File.Exists(fullPath))
            return (false, relativePath);

        var normalized = (unifiedDiff ?? "").Replace("\r\n", "\n").Replace("\r", "\n");
        if (string.IsNullOrWhiteSpace(normalized))
            return (false, relativePath);

        var lines = normalized.Split('\n');
        string? removeLine = null;
        string? addLine = null;
        for (var i = 0; i < lines.Length; i++)
        {
            var l = lines[i];
            if (l.StartsWith("---") || l.StartsWith("+++") || l.StartsWith("@@")) continue;
            if (l.StartsWith("-") && !l.StartsWith("---")) removeLine = l[1..];
            if (l.StartsWith("+") && !l.StartsWith("+++")) addLine = l[1..];
        }

        if (string.IsNullOrEmpty(removeLine) || addLine == null)
            return (false, relativePath);
        if (removeLine == addLine)
            return (false, relativePath);

        var content = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(content))
            return (false, relativePath);

        var updated = ReplaceUniqueOccurrence(content, removeLine, addLine);
        if (updated == null)
            return (false, relativePath);

        await File.WriteAllTextAsync(fullPath, updated, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
        return (true, relativePath.Replace('\\', '/').TrimStart('/'));
    }

    private static string? ReplaceUniqueOccurrence(string content, string oldValue, string newValue)
    {
        var first = content.IndexOf(oldValue, StringComparison.Ordinal);
        if (first >= 0)
        {
            var second = content.IndexOf(oldValue, first + oldValue.Length, StringComparison.Ordinal);
            if (second < 0)
                return content[..first] + newValue + content[(first + oldValue.Length)..];
        }

        // Exact satır değişimi mümkün değilse trim eşleşmesiyle tek satırı güncelle.
        var lines = content.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var oldTrim = oldValue.Trim();
        var matchIdx = -1;
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim() == oldTrim)
            {
                if (matchIdx != -1) return null; // birden fazla aday varsa güvenli değil
                matchIdx = i;
            }
        }
        if (matchIdx < 0) return null;

        var original = lines[matchIdx];
        var indentLength = original.Length - original.TrimStart().Length;
        var indent = indentLength > 0 ? original[..indentLength] : string.Empty;
        lines[matchIdx] = indent + newValue.TrimStart();
        var joined = string.Join("\n", lines);
        if (content.EndsWith("\n", StringComparison.Ordinal) && !joined.EndsWith("\n", StringComparison.Ordinal))
            joined += "\n";
        return joined;
    }

    public async Task<string> RunBuildAsync(CancellationToken cancellationToken = default)
    {
        var repo = RepoPath;
        if (string.IsNullOrEmpty(repo))
            return "HATA: Git:RepoPath yapılandırılmamış; build alınamıyor.";
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build --no-restore",
                WorkingDirectory = repo,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process == null)
                return "HATA: dotnet build başlatılamadı.";
            var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var output = (stdout + "\n" + stderr).Trim();
            if (process.ExitCode != 0)
            {
                if (IsEnvironmentBuildFailure(output))
                    return BuildSkippedPrefix + " Derleme ortam nedeniyle tamamlanamadı (SDK eksik veya dosya kilidi/MSB302x vb.); push akışında build atlanacak.\n\n" +
                           (output.Length > 4000 ? output.AsSpan(0, 4000).ToString() + "\n... (kesildi)" : output);
                return "BUILD_HATA: Derleme başarısız (exit code " + process.ExitCode + "). Kullanıcıya bu çıktıyı göster, pushlama; düzeltmesini veya PR açmasını öner.\n\n" + (output.Length > 6000 ? output.AsSpan(0, 6000).ToString() + "\n... (kesildi)" : output);
            }

            return "OK: Build başarılı.\n" + (output.Length > 2000 ? output.Substring(output.Length - 2000) : output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AgentTools.RunBuildAsync: dotnet build hatası.");
            return "HATA: Build çalıştırılamadı: " + ex.Message;
        }
    }

    public async Task<string> ConfirmPushAsync(int conversationId, CancellationToken cancellationToken = default)
    {
        var pending = _pendingPush.Get(conversationId);
        if (pending == null)
            return "HATA: Bu konuşma için onay bekleyen değişiklik yok. Önce apply_diff veya write_file ile değişiklik yapıp kullanıcı onayı alın.";
        var (paths, commitMessage, _) = pending.Value;

        if (IsPrePushBuildDisabled())
        {
            _logger.LogWarning("AgentTools.ConfirmPushAsync: ArabamAi:PrePushBuild=false; build atlanıyor. ConversationId: {ConversationId}", conversationId);
            _pendingPush.Clear(conversationId);
            var pushOnly = await _gitService.CommitAndPushAsync(paths, commitMessage, cancellationToken);
            if (pushOnly.Success)
                return "OK: Ön derleme kapalı (ArabamAi:PrePushBuild=false); değişiklikler commit edilip main'e pushlandı: " + pushOnly.Message;
            return "HATA: Push başarısız: " + pushOnly.Message;
        }

        var buildResult = await RunBuildAsync(cancellationToken);
        if (buildResult.StartsWith(BuildSkippedPrefix, StringComparison.Ordinal))
        {
            _logger.LogWarning("AgentTools.ConfirmPushAsync: Build ortam nedeniyle atlandı (SDK/dosya kilidi vb.). ConversationId: {ConversationId}", conversationId);
            _pendingPush.Clear(conversationId);
            var skippedBuildPushResult = await _gitService.CommitAndPushAsync(paths, commitMessage, cancellationToken);
            if (skippedBuildPushResult.Success)
                return "OK: Build ortamda atlandı (SDK eksik veya dosya kilidi vb.), değişiklikler commit edilip main'e pushlandı: " + skippedBuildPushResult.Message;
            return "HATA: Build atlandı ama push başarısız: " + skippedBuildPushResult.Message;
        }
        if (buildResult.StartsWith("BUILD_HATA:") || buildResult.StartsWith("HATA:"))
        {
            return buildResult + "\n\nDeğişiklikler pushlanmadı. Hatayı düzelttikten sonra tekrar 'Evet, pushla' diyebilir veya 'PR aç' diyerek sadece branch oluşturup PR açabilirsiniz.";
        }

        _pendingPush.Clear(conversationId);
        var result = await _gitService.CommitAndPushAsync(paths, commitMessage, cancellationToken);
        if (result.Success)
            return "OK: Build başarılı, değişiklikler commit edilip main'e pushlandı: " + result.Message;
        return "HATA: Push başarısız: " + result.Message;
    }

    private bool IsPrePushBuildDisabled()
    {
        var v = _config["ArabamAi:PrePushBuild"]?.Trim();
        return string.Equals(v, "false", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Bu çıktı gerçek kod hatası değil; yerelde SDK yok veya başka işlem çıktı dosyalarını kilitliyor gibi durumlarda true.</summary>
    private static bool IsEnvironmentBuildFailure(string output)
    {
        if (string.IsNullOrWhiteSpace(output)) return false;
        if (IsDotnetSdkMissing(output)) return true;
        return output.Contains("MSB3021", StringComparison.OrdinalIgnoreCase)
               || output.Contains("MSB3027", StringComparison.OrdinalIgnoreCase)
               || output.Contains("MSB3026", StringComparison.OrdinalIgnoreCase)
               || output.Contains("being used by another process", StringComparison.OrdinalIgnoreCase)
               || output.Contains("Could not copy", StringComparison.OrdinalIgnoreCase)
               || output.Contains("The process cannot access the file", StringComparison.OrdinalIgnoreCase)
               || output.Contains("Access to the path", StringComparison.OrdinalIgnoreCase)
               || output.Contains("file is locked", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDotnetSdkMissing(string output)
    {
        if (string.IsNullOrWhiteSpace(output)) return false;
        return output.Contains("A compatible .NET SDK was not found", StringComparison.OrdinalIgnoreCase)
               || output.Contains("No .NET SDKs were found", StringComparison.OrdinalIgnoreCase)
               || output.Contains("It was not possible to find any installed .NET SDKs", StringComparison.OrdinalIgnoreCase)
               || (output.Contains("The command could not be loaded", StringComparison.OrdinalIgnoreCase)
                   && output.Contains("SDK", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<string> CreatePrAsync(int conversationId, CancellationToken cancellationToken = default)
    {
        var pending = _pendingPush.Get(conversationId);
        if (pending == null)
            return "HATA: Bu konuşma için onay bekleyen değişiklik yok. Önce apply_diff veya write_file ile değişiklik yapıp kullanıcıya 'PR aç' dedirt.";
        var (paths, commitMessage, _) = pending.Value;

        var branchName = "feature/arabam-ai-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmm");
        _pendingPush.Clear(conversationId);
        var result = await _gitService.CreateBranchAndPushAsync(branchName, paths, commitMessage, cancellationToken);
        if (!result.Success)
            return "HATA: " + result.Message;

        var prResult = await _gitService.CreateGitHubPullRequestAsync(branchName, commitMessage, null, cancellationToken);
        if (prResult.Success)
            return "OK: PR oluşturuldu. Branch: " + branchName + ".\n\n[PR linki](" + prResult.Message + ")";
        return "OK: Branch pushlandı: " + branchName + ". GitHub PR otomatik açılamadı (" + prResult.Message + "). GitHub'da 'Compare & pull request' ile PR açabilirsiniz.";
    }

    public async Task<string> GitCommitAndPushAsync(string commitMessage, IReadOnlyList<string> relativePaths, CancellationToken cancellationToken = default)
    {
        if (relativePaths.Count == 0)
            return "HATA: En az bir dosya gerekli.";
        var result = await _gitService.CommitAndPushAsync(relativePaths, commitMessage, cancellationToken);
        return result.Success ? "OK: " + result.Message : "HATA: " + result.Message;
    }

    public Task<string> ProposeSqlAsync(string sql, int conversationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var guard = SqlSelectGuard.ValidateAndNormalize(sql);
        if (!guard.IsValid || string.IsNullOrEmpty(guard.NormalizedSql))
            return Task.FromResult("HATA: " + (guard.ErrorMessage ?? "SQL doğrulanamadı."));

        _pendingSql.Set(conversationId, guard.NormalizedSql);
        var msg =
            "OK: Sorgu bu konuşma için kaydedildi (en fazla " + SqlSelectGuard.MaxRows + " satır). Kullanıcıya aşağıdaki SQL'i kod bloğunda göster ve çalıştırmadan önce açık onay iste (ör. «Evet, çalıştır», «Onaylıyorum»). Onay sonrası yalnızca execute_pending_sql çağır; başka parametre veya ham SQL ile veritabanına gitme.\n\n```sql\n" +
            guard.NormalizedSql +
            "\n```";
        return Task.FromResult(msg);
    }

    public async Task<string> ExecutePendingSqlAsync(int conversationId, CancellationToken cancellationToken = default)
    {
        var pending = _pendingSql.Get(conversationId);
        if (string.IsNullOrEmpty(pending))
            return "HATA: Bu konuşma için onay bekleyen SELECT sorgusu yok. Önce propose_sql ile geçerli bir sorgu önerin ve kullanıcıdan onay alın.";

        try
        {
            var output = await _readOnlySql.ExecuteSelectAsync(pending, cancellationToken).ConfigureAwait(false);
            if (output.StartsWith("HATA:", StringComparison.Ordinal))
                return output;
            _pendingSql.Clear(conversationId);
            return output;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AgentTools.ExecutePendingSqlAsync: Sorgu çalıştırılamadı. ConversationId: {Id}", conversationId);
            return "HATA: Sorgu çalıştırılamadı: " + ex.Message;
        }
    }
}
