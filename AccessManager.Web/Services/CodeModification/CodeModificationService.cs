using System.Text;
using AccessManager.UI.Services.Git;

namespace AccessManager.UI.Services.CodeModification;

public sealed class CodeModificationService : ICodeModificationService
{
    private readonly IConfiguration _config;
    private readonly IGitService _gitService;

    public CodeModificationService(IConfiguration config, IGitService gitService)
    {
        _config = config;
        _gitService = gitService;
    }

    private string RepoPath
    {
        get
        {
            var path = _config["Git:RepoPath"]?.Trim();
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException("Git:RepoPath yapılandırması gerekli.");
            return Path.GetFullPath(path);
        }
    }

    public async Task<ApplyDiffResult> ApplyDiffAsync(string relativePath, string unifiedDiff, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || relativePath.Contains(".."))
            return new ApplyDiffResult { Success = false, Message = "Geçersiz dosya yolu." };

        var repo = RepoPath;
        var fullPath = Path.GetFullPath(Path.Combine(repo, relativePath));
        if (!fullPath.StartsWith(repo, StringComparison.OrdinalIgnoreCase))
            return new ApplyDiffResult { Success = false, Message = "Dosya repo dışında." };

        // Unified diff genelde --- a/path ve +++ b/path ile başlar; git apply bunu kabul eder
        var patchContent = unifiedDiff.Trim();
        if (string.IsNullOrEmpty(patchContent))
            return new ApplyDiffResult { Success = false, Message = "Boş diff." };

        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, patchContent, Encoding.UTF8, cancellationToken);
            var (success, output) = await RunGitApplyAsync(repo, tempFile, cancellationToken);
            return new ApplyDiffResult
            {
                Success = success,
                Message = output ?? (success ? "Diff uygulandı." : "Bilinmeyen hata.")
            };
        }
        finally
        {
            try { File.Delete(tempFile); } catch { /* ignore */ }
        }
    }

    public async Task<CodeModificationResult> ApplyDiffsAndPushAsync(
        IReadOnlyList<FileDiffInput> files,
        string commitMessage,
        CancellationToken cancellationToken = default)
    {
        if (files.Count == 0)
            return new CodeModificationResult { Success = false, Message = "Dosya listesi boş." };

        var repo = RepoPath;
        var paths = new List<string>();

        foreach (var f in files)
        {
            var result = await ApplyDiffAsync(f.Path, f.Diff, cancellationToken);
            if (!result.Success)
                return new CodeModificationResult { Success = false, Message = $"{f.Path}: {result.Message}" };
            paths.Add(f.Path);
        }

        var gitResult = await _gitService.CommitAndPushAsync(paths, commitMessage, cancellationToken);
        return new CodeModificationResult
        {
            Success = gitResult.Success,
            Message = gitResult.Message
        };
    }

    private static async Task<(bool Success, string? Output)> RunGitApplyAsync(string repoPath, string patchFilePath, CancellationToken ct)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"apply --ignore-whitespace \"{patchFilePath}\"",
            WorkingDirectory = repoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        try
        {
            using var process = System.Diagnostics.Process.Start(psi);
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
