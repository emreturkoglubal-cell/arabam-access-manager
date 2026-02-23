using AccessManager.UI.Services.CodeModification;
using AccessManager.UI.Services.Git;

namespace AccessManager.UI.Services.Agent;

public sealed class AgentTools : IAgentTools
{
    private readonly IConfiguration _config;
    private readonly ICodeModificationService _codeMod;
    private readonly IGitService _gitService;
    private readonly IPendingPushStore _pendingPush;
    private readonly ILogger<AgentTools> _logger;

    public AgentTools(
        IConfiguration config,
        ICodeModificationService codeMod,
        IGitService gitService,
        IPendingPushStore pendingPush,
        ILogger<AgentTools> logger)
    {
        _config = config;
        _codeMod = codeMod;
        _gitService = gitService;
        _pendingPush = pendingPush;
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
            return "HATA: " + result.Message;
        }

        var pathForGit = (result.ResolvedPath ?? relativePath).Replace("\\", "/");
        if (conversationId.HasValue && conversationId.Value > 0)
        {
            var diffStored = unifiedDiff.Length > 8000 ? unifiedDiff[..8000] + "\n... (kesildi)" : unifiedDiff;
            _pendingPush.Set(conversationId.Value, new[] { pathForGit }, "Kod güncellemesi (arabam AI)", diffStored);
        }
        return "OK: Diff uygulandı. Değiştirilen dosya: " + pathForGit + ". Kullanıcıya aşağıdaki diff'i kod bloğunda gösterip onay iste. Onay gelince confirm_and_push çağır. ASLA bu yanıtta git_commit_and_push çağırma.\n\nDiff:\n```diff\n" + (unifiedDiff.Length > 8000 ? unifiedDiff[..8000] + "\n... (kesildi)" : unifiedDiff) + "\n```";
    }

    public async Task<string> ConfirmPushAsync(int conversationId, CancellationToken cancellationToken = default)
    {
        var pending = _pendingPush.Get(conversationId);
        if (pending == null)
            return "HATA: Bu konuşma için onay bekleyen değişiklik yok. Önce apply_diff veya write_file ile değişiklik yapıp kullanıcı onayı alın.";
        var (paths, commitMessage, _) = pending.Value;
        _pendingPush.Clear(conversationId);
        var result = await _gitService.CommitAndPushAsync(paths, commitMessage, cancellationToken);
        if (result.Success)
            return "OK: Değişiklikler commit edilip main'e pushlandı: " + result.Message;
        return "HATA: Push başarısız: " + result.Message;
    }

    public async Task<string> GitCommitAndPushAsync(string commitMessage, IReadOnlyList<string> relativePaths, CancellationToken cancellationToken = default)
    {
        if (relativePaths.Count == 0)
            return "HATA: En az bir dosya gerekli.";
        var result = await _gitService.CommitAndPushAsync(relativePaths, commitMessage, cancellationToken);
        return result.Success ? "OK: " + result.Message : "HATA: " + result.Message;
    }
}
