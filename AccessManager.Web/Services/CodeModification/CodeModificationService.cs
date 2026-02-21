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
        var pathNorm = relativePath.Replace('\\', '/').TrimStart('/');
        var fullPath = Path.GetFullPath(Path.Combine(repo, pathNorm));
        if (!fullPath.StartsWith(repo, StringComparison.OrdinalIgnoreCase))
            return new ApplyDiffResult { Success = false, Message = "Dosya repo dışında." };

        // Model bazen Pages gönderiyor; bu projede view'lar Views altında (Views/Systems/Index.cshtml)
        if (!File.Exists(fullPath) && pathNorm.Contains("Pages", StringComparison.OrdinalIgnoreCase))
        {
            var viewsPath = pathNorm.Replace("/Pages/", "/Views/").Replace("\\Pages\\", "\\Views\\");
            var fullPathViews = Path.GetFullPath(Path.Combine(repo, viewsPath));
            if (fullPathViews.StartsWith(repo, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPathViews))
            {
                fullPath = fullPathViews;
                pathNorm = viewsPath;
            }
        }
        if (!File.Exists(fullPath))
            return new ApplyDiffResult { Success = false, Message = "Dosya bulunamadı: " + fullPath };

        var patchContent = unifiedDiff.Replace("\r\n", "\n").Replace("\r", "\n").Trim();
        if (string.IsNullOrEmpty(patchContent))
            return new ApplyDiffResult { Success = false, Message = "Boş diff." };

        // Dosyayı doğrudan okuyup patch'i bellekte uyguluyoruz (git apply'a bağlı değiliz)
        string currentContent;
        try
        {
            currentContent = await File.ReadAllTextAsync(fullPath, Encoding.UTF8, cancellationToken);
        }
        catch (Exception ex)
        {
            return new ApplyDiffResult { Success = false, Message = "Dosya okunamadı: " + ex.Message };
        }

        var (applied, newContent, error) = ApplyUnifiedDiffInMemory(currentContent, patchContent);
        if (!applied)
            return new ApplyDiffResult { Success = false, Message = error ?? "Patch uygulanamadı." };

        try
        {
            await File.WriteAllTextAsync(fullPath, newContent, new UTF8Encoding(false), cancellationToken);
        }
        catch (Exception ex)
        {
            return new ApplyDiffResult { Success = false, Message = "Dosya yazılamadı: " + ex.Message };
        }

        var pathForResult = pathNorm.Replace("\\", "/");
        return new ApplyDiffResult
        {
            Success = true,
            Message = $"Dosya güncellendi: {pathForResult} (tam yol: {fullPath})",
            ResolvedPath = pathNorm.Replace("\\", "/")
        };
    }

    /// <summary>
    /// Unified diff'i bellekte uygular; git'e ihtiyaç yok. Dosya içeriği kesin değişir.
    /// </summary>
    private static (bool Success, string? NewContent, string? Error) ApplyUnifiedDiffInMemory(string fileContent, string patchContent)
    {
        var fileLines = fileContent.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n').ToList();
        var patchLines = patchContent.Split('\n');
        var outLines = new List<string>();
        var fileIndex = 0;
        var i = 0;

        while (i < patchLines.Length)
        {
            var line = patchLines[i];
            if (line.StartsWith("---") || line.StartsWith("+++"))
            {
                i++;
                continue;
            }
            if (line.StartsWith("@@"))
            {
                // @@ -oldStart,oldCount +newStart,newCount @@
                if (!TryParseHunkHeader(line, out var oldStart, out var oldCount))
                {
                    i++;
                    continue;
                }
                i++;
                if (oldStart < 1) oldStart = 1;
                var oldStart0 = oldStart - 1;

                if (fileIndex < oldStart0)
                {
                    for (var j = fileIndex; j < oldStart0 && j < fileLines.Count; j++)
                        outLines.Add(fileLines[j]);
                    fileIndex = oldStart0;
                }

                var oldEnd = oldStart0 + oldCount;
                var hunkLineCount = 0;
                while (i < patchLines.Length && !patchLines[i].StartsWith("@@") && !patchLines[i].StartsWith("---"))
                {
                    var hunkLine = patchLines[i];
                    i++;
                    if (hunkLine.Length == 0)
                    {
                        if (fileIndex < fileLines.Count && fileLines[fileIndex] == "")
                        {
                            outLines.Add("");
                            fileIndex++;
                        }
                        else if (fileIndex < fileLines.Count)
                            return (false, null, $"Satır {fileIndex + 1}: boş satır bekleniyor, dosyada: '{fileLines[fileIndex]}'.");
                        continue;
                    }
                    var op = hunkLine[0];
                    var rest = hunkLine.Length > 1 ? hunkLine[1..] : "";
                    if (op == ' ')
                    {
                        if (fileIndex >= fileLines.Count)
                            return (false, null, $"Satır {fileIndex + 1}: bağlam eşleşmedi (dosya kısa).");
                        if (!LineMatches(fileLines[fileIndex], rest))
                        {
                            // Patch satır numarası yanlış veya boşluk farklı; dosyada bu bağlamı ara (trim ile)
                            var found = -1;
                            for (var k = fileIndex; k < fileLines.Count; k++)
                            {
                                if (LineMatches(fileLines[k], rest)) { found = k; break; }
                            }
                            if (found < 0)
                                return (false, null, $"Bağlam bulunamadı. Beklenen satır: '{rest}'. Patch satır numarası yanlış olabilir.");
                            for (var k = fileIndex; k < found; k++)
                                outLines.Add(fileLines[k]);
                            fileIndex = found;
                        }
                        outLines.Add(fileLines[fileIndex]);
                        fileIndex++;
                    }
                    else if (op == '-')
                    {
                        if (fileIndex >= fileLines.Count)
                            return (false, null, $"Satır {fileIndex + 1}: silinecek satır yok.");
                        if (!LineMatches(fileLines[fileIndex], rest))
                        {
                            var found = -1;
                            for (var k = fileIndex; k < fileLines.Count; k++)
                            {
                                if (LineMatches(fileLines[k], rest)) { found = k; break; }
                            }
                            if (found < 0)
                                return (false, null, $"Silinecek satır bulunamadı: '{rest}'.");
                            for (var k = fileIndex; k < found; k++)
                                outLines.Add(fileLines[k]);
                            fileIndex = found + 1; // satırı atla (sil)
                            continue;
                        }
                        fileIndex++;
                    }
                    else if (op == '+')
                    {
                        outLines.Add(rest);
                    }
                    hunkLineCount++;
                }
                continue;
            }
            i++;
        }

        for (; fileIndex < fileLines.Count; fileIndex++)
            outLines.Add(fileLines[fileIndex]);

        var newContent = string.Join("\n", outLines);
        if (fileContent.EndsWith("\n") && !newContent.EndsWith("\n"))
            newContent += "\n";
        return (true, newContent, null);
    }

    /// <summary>Satır eşleşmesi: tam veya trim edilmiş (patch bazen boşluksuz gönderiyor, dosyada girintili olabiliyor).</summary>
    private static bool LineMatches(string fileLine, string patchLine)
    {
        if (fileLine == patchLine) return true;
        return fileLine.Trim() == patchLine.Trim();
    }

    private static bool TryParseHunkHeader(string line, out int oldStart, out int oldCount)
    {
        oldStart = 0;
        oldCount = 0;
        var idx = line.IndexOf(' ');
        if (idx < 0) return false;
        line = line[(idx + 1)..];
        idx = line.IndexOf(' ');
        if (idx < 0) return false;
        var oldPart = line[..idx];
        var comma = oldPart.IndexOf(',');
        if (comma < 0) return false;
        if (!int.TryParse(oldPart.AsSpan(1, comma - 1), out oldStart)) return false;
        if (!int.TryParse(oldPart.AsSpan(comma + 1), out oldCount)) return false;
        return true;
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

}
