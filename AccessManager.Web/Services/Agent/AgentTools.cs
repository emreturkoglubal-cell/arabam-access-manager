using AccessManager.UI.Services.CodeModification;
using AccessManager.UI.Services.Git;

namespace AccessManager.UI.Services.Agent;

public sealed class AgentTools : IAgentTools
{
    private readonly IConfiguration _config;
    private readonly ICodeModificationService _codeMod;
    private readonly IGitService _gitService;

    public AgentTools(
        IConfiguration config,
        ICodeModificationService codeMod,
        IGitService gitService)
    {
        _config = config;
        _codeMod = codeMod;
        _gitService = gitService;
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
        if (!TryResolvePath(relativePath, out var fullPath, out var error))
            return "HATA: " + error;
        try
        {
            if (!File.Exists(fullPath))
                return "HATA: Dosya bulunamadı: " + relativePath;
            return File.ReadAllText(fullPath);
        }
        catch (Exception ex)
        {
            return "HATA: " + ex.Message;
        }
    }

    public string WriteFile(string relativePath, string content)
    {
        if (!TryResolvePath(relativePath, out var fullPath, out var error))
            return "HATA: " + error;
        try
        {
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(fullPath, content ?? string.Empty);
            return "OK: Dosya yazıldı.";
        }
        catch (Exception ex)
        {
            return "HATA: " + ex.Message;
        }
    }

    public async Task<string> ApplyDiffAsync(string relativePath, string unifiedDiff, CancellationToken cancellationToken = default)
    {
        var result = await _codeMod.ApplyDiffAsync(relativePath, unifiedDiff, cancellationToken);
        return result.Success ? "OK: " + result.Message : "HATA: " + result.Message;
    }

    public async Task<string> GitCommitAndPushAsync(string commitMessage, IReadOnlyList<string> relativePaths, CancellationToken cancellationToken = default)
    {
        if (relativePaths.Count == 0)
            return "HATA: En az bir dosya gerekli.";
        var result = await _gitService.CommitAndPushAsync(relativePaths, commitMessage, cancellationToken);
        return result.Success ? "OK: " + result.Message : "HATA: " + result.Message;
    }
}
