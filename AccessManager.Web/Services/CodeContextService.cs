using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AccessManager.UI.Services;

public class CodeContextService : ICodeContextService
{
    private const int MaxStructureFiles = 2500;
    private const string StructureCacheKey = "CodeContext:ProjectStructure";
    private static readonly TimeSpan StructureCacheDuration = TimeSpan.FromMinutes(5);

    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly ILogger<CodeContextService> _logger;
    private readonly IMemoryCache _cache;
    private static readonly string[] AllowedExtensions = { ".cs", ".cshtml", ".json" };
    private static readonly string[] SkipDirs = { "obj", "bin", "node_modules", ".git", "lib", "wwwroot/lib" };

    public CodeContextService(IWebHostEnvironment env, IConfiguration config, ILogger<CodeContextService> logger, IMemoryCache cache)
    {
        _env = env;
        _config = config;
        _logger = logger;
        _cache = cache;
    }

    public Task<string> GetCodeContextAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var basePath = _config["CodeContext:BasePath"]?.Trim();
            if (string.IsNullOrEmpty(basePath))
                basePath = Path.GetDirectoryName(_env.ContentRootPath) ?? _env.ContentRootPath;

            if (!Directory.Exists(basePath))
                return Task.FromResult("# Kaynak klasörü bulunamadı: " + basePath);

            var maxChars = _config.GetValue("CodeContext:MaxCharacters", 40_000);
            var sb = new StringBuilder(maxChars + 5000);

            foreach (var dir in Directory.EnumerateDirectories(basePath))
            {
                var dirName = Path.GetFileName(dir);
                if (SkipDirs.Any(s => dirName.Equals(s, StringComparison.OrdinalIgnoreCase)))
                    continue;
                AppendDirectory(dir, sb, basePath, maxChars);
                if (sb.Length >= maxChars) break;
            }

            foreach (var file in Directory.EnumerateFiles(basePath))
            {
                if (!AllowedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                    continue;
                AppendFile(file, basePath, sb, maxChars);
                if (sb.Length >= maxChars) break;
            }

            if (sb.Length == 0)
                sb.Append("# Bu dizinde uygun kaynak dosyası bulunamadı.");
            return Task.FromResult(sb.ToString());
        }
        catch (OutOfMemoryException ex)
        {
            _logger.LogError(ex, "CodeContextService.GetCodeContextAsync: Bellek yetersiz (OOM). BasePath veya dizin çok büyük olabilir.");
            return Task.FromResult("# Proje yapısı yüklenirken bellek sınırı aşıldı. Daha küçük bir dizin veya CodeContext:BasePath kullanın.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CodeContextService.GetCodeContextAsync: Proje yapısı okunurken hata. BasePath: {BasePath}", _config["CodeContext:BasePath"]);
            return Task.FromResult("# Proje yapısı okunamadı: " + ex.Message);
        }
    }

    public Task<string> GetProjectStructureAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(StructureCacheKey, out string? cached) && cached != null)
            return Task.FromResult(cached);

        try
        {
            var basePath = _config["CodeContext:BasePath"]?.Trim();
            if (string.IsNullOrEmpty(basePath))
                basePath = Path.GetDirectoryName(_env.ContentRootPath) ?? _env.ContentRootPath;

            if (!Directory.Exists(basePath))
                return Task.FromResult("# Repo kökü bulunamadı: " + basePath);

            var sb = new StringBuilder(4000);
            sb.AppendLine("# Proje yapısı (repo köküne göre relative path). Detay için read_file kullan.");
            sb.AppendLine();

            foreach (var dir in Directory.EnumerateDirectories(basePath))
            {
                var dirName = Path.GetFileName(dir);
                if (SkipDirs.Any(s => dirName.Equals(s, StringComparison.OrdinalIgnoreCase)))
                    continue;
                AppendStructureDir(dir, basePath, sb);
            }

            foreach (var file in Directory.EnumerateFiles(basePath))
            {
                if (!AllowedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                    continue;
                sb.AppendLine(Path.GetRelativePath(basePath, file));
            }

            var result = sb.ToString();
            _cache.Set(StructureCacheKey, result, StructureCacheDuration);
            return Task.FromResult(result);
        }
        catch (OutOfMemoryException ex)
        {
            _logger.LogError(ex, "CodeContextService.GetProjectStructureAsync: Bellek yetersiz (OOM). Repo çok büyük; EnumerateFiles ile sınırlı sayıda dosya kullanılıyor olmalı.");
            return Task.FromResult("# Proje yapısı alınırken bellek sınırı aşıldı. Canlı ortamda repo boyutunu veya CodeContext:BasePath'i kısıtlayın.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CodeContextService.GetProjectStructureAsync: Proje yapısı üretilirken hata. BasePath: {BasePath}", _config["CodeContext:BasePath"]);
            return Task.FromResult("# Proje yapısı alınamadı: " + ex.Message);
        }
    }

    private void AppendStructureDir(string dirPath, string basePath, StringBuilder sb)
    {
        int count = 0;
        foreach (var file in Directory.EnumerateFiles(dirPath, "*.*", SearchOption.AllDirectories))
        {
            if (count >= MaxStructureFiles)
            {
                sb.AppendLine("... (çok fazla dosya, listeleme " + MaxStructureFiles + " ile sınırlandı)");
                return;
            }
            count++;
            var ext = Path.GetExtension(file);
            if (!AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                continue;
            var relative = Path.GetRelativePath(basePath, file);
            if (SkipDirs.Any(s => relative.Contains(Path.DirectorySeparatorChar + s + Path.DirectorySeparatorChar)
                || relative.StartsWith(s + Path.DirectorySeparatorChar, StringComparison.Ordinal)))
                continue;
            sb.AppendLine(relative.Replace('\\', '/'));
        }
    }

    private void AppendDirectory(string dirPath, StringBuilder sb, string basePath, int maxChars)
    {
        foreach (var file in Directory.EnumerateFiles(dirPath, "*.*", SearchOption.AllDirectories))
        {
            if (sb.Length >= maxChars) return;

            var ext = Path.GetExtension(file);
            if (!AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                continue;

            var relative = Path.GetRelativePath(basePath, file);
            if (SkipDirs.Any(s => relative.Contains(Path.DirectorySeparatorChar + s + Path.DirectorySeparatorChar)
                || relative.StartsWith(s + Path.DirectorySeparatorChar, StringComparison.Ordinal)))
                continue;

            AppendFile(file, basePath, sb, maxChars);
        }
    }

    private static void AppendFile(string filePath, string basePath, StringBuilder sb, int maxChars)
    {
        var relative = Path.GetRelativePath(basePath, filePath);
        sb.AppendLine("\n--- ");
        sb.AppendLine("Dosya: " + relative);
        sb.AppendLine("---");
        try
        {
            var content = File.ReadAllText(filePath);
            if (content.Length + sb.Length > maxChars)
                content = content.AsSpan(0, maxChars - sb.Length - 50).ToString() + "\n... (kesildi)";
            sb.AppendLine(content);
        }
        catch
        {
            sb.AppendLine("(okunamadı)");
        }
    }
}
