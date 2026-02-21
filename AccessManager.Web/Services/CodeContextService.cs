using System.Text;

namespace AccessManager.UI.Services;

public class CodeContextService : ICodeContextService
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private static readonly string[] AllowedExtensions = { ".cs", ".cshtml", ".json" };
    private static readonly string[] SkipDirs = { "obj", "bin", "node_modules", ".git", "lib", "wwwroot/lib" };

    public CodeContextService(IWebHostEnvironment env, IConfiguration config)
    {
        _env = env;
        _config = config;
    }

    public Task<string> GetCodeContextAsync(CancellationToken cancellationToken = default)
    {
        var basePath = _config["CodeContext:BasePath"]?.Trim();
        if (string.IsNullOrEmpty(basePath))
            basePath = Path.GetDirectoryName(_env.ContentRootPath) ?? _env.ContentRootPath;

        if (!Directory.Exists(basePath))
            return Task.FromResult("# Kaynak klasörü bulunamadı: " + basePath);

        var maxChars = _config.GetValue("CodeContext:MaxCharacters", 40_000);
        var sb = new StringBuilder(maxChars + 5000);

        foreach (var dir in Directory.GetDirectories(basePath))
        {
            var dirName = Path.GetFileName(dir);
            if (SkipDirs.Any(s => dirName.Equals(s, StringComparison.OrdinalIgnoreCase)))
                continue;

            AppendDirectory(dir, sb, basePath, maxChars);
            if (sb.Length >= maxChars) break;
        }

        // Kök dizindeki dosyalar (sln, global.json vb.)
        foreach (var file in Directory.GetFiles(basePath))
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

    public Task<string> GetProjectStructureAsync(CancellationToken cancellationToken = default)
    {
        var basePath = _config["CodeContext:BasePath"]?.Trim();
        if (string.IsNullOrEmpty(basePath))
            basePath = Path.GetDirectoryName(_env.ContentRootPath) ?? _env.ContentRootPath;

        if (!Directory.Exists(basePath))
            return Task.FromResult("# Repo kökü bulunamadı: " + basePath);

        var sb = new StringBuilder(4000);
        sb.AppendLine("# Proje yapısı (repo köküne göre relative path). Detay için read_file kullan.");
        sb.AppendLine();

        foreach (var dir in Directory.GetDirectories(basePath))
        {
            var dirName = Path.GetFileName(dir);
            if (SkipDirs.Any(s => dirName.Equals(s, StringComparison.OrdinalIgnoreCase)))
                continue;
            AppendStructureDir(dir, basePath, sb);
        }

        foreach (var file in Directory.GetFiles(basePath))
        {
            if (!AllowedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                continue;
            sb.AppendLine(Path.GetRelativePath(basePath, file));
        }

        return Task.FromResult(sb.ToString());
    }

    private void AppendStructureDir(string dirPath, string basePath, StringBuilder sb)
    {
        foreach (var file in Directory.GetFiles(dirPath, "*.*", SearchOption.AllDirectories))
        {
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
        foreach (var file in Directory.GetFiles(dirPath, "*.*", SearchOption.AllDirectories))
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
