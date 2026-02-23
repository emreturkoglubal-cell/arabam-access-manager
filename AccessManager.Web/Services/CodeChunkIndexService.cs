using AccessManager.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace AccessManager.UI.Services;

/// <summary>
/// Repo dosyalarını tarayıp içerikleri embed ederek code_chunks tablosuna yazar (RAG index).
/// </summary>
public class CodeChunkIndexService
{
    private const int MaxStructureFiles = 2500;
    private const int MaxContentLength = 25000; // embedding input limitine uygun
    private const int EmbeddingBatchSize = 20;

    private static readonly string[] SkipDirs = { "obj", "bin", "node_modules", ".git", "lib", "wwwroot/lib" };
    private static readonly string[] AllowedExtensions = { ".cs", ".cshtml", ".json" };

    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ICodeChunkRepository _repo;
    private readonly IEmbeddingService _embedding;
    private readonly ILogger<CodeChunkIndexService> _logger;

    public CodeChunkIndexService(
        IConfiguration config,
        IWebHostEnvironment env,
        ICodeChunkRepository repo,
        IEmbeddingService embedding,
        ILogger<CodeChunkIndexService> logger)
    {
        _config = config;
        _env = env;
        _repo = repo;
        _embedding = embedding;
        _logger = logger;
    }

    /// <summary>Tüm code_chunks'ı siler, repo'yu tarar, chunk'ları embed edip tabloya yazar.</summary>
    public async Task<int> ReindexAsync(CancellationToken cancellationToken = default)
    {
        var basePath = _config["CodeContext:BasePath"]?.Trim();
        if (string.IsNullOrEmpty(basePath))
            basePath = Path.GetDirectoryName(_env.ContentRootPath) ?? _env.ContentRootPath;

        if (!Directory.Exists(basePath))
        {
            _logger.LogWarning("CodeChunkIndexService: Base path bulunamadı: {BasePath}", basePath);
            return 0;
        }

        var chunks = new List<(string path, string content)>();
        var count = 0;

        foreach (var dir in Directory.EnumerateDirectories(basePath))
        {
            var dirName = Path.GetFileName(dir);
            if (SkipDirs.Any(s => dirName.Equals(s, StringComparison.OrdinalIgnoreCase))) continue;
            CollectChunksFromDir(dir, basePath, chunks);
            if (chunks.Count >= MaxStructureFiles) break;
        }

        foreach (var file in Directory.EnumerateFiles(basePath))
        {
            if (count >= MaxStructureFiles) break;
            if (!AllowedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)) continue;
            var relative = Path.GetRelativePath(basePath, file).Replace('\\', '/');
            var content = ReadFileContent(file);
            if (!string.IsNullOrEmpty(content)) chunks.Add((relative, content));
            count++;
        }

        if (chunks.Count == 0)
        {
            _logger.LogInformation("CodeChunkIndexService: İndexlenecek dosya yok.");
            return 0;
        }

        await _repo.DeleteAllAsync(cancellationToken);

        var total = 0;
        for (var i = 0; i < chunks.Count; i += EmbeddingBatchSize)
        {
            var batch = chunks.Skip(i).Take(EmbeddingBatchSize).ToList();
            var texts = batch.Select(c => c.content).ToList();
            var embeddings = await _embedding.GetEmbeddingsAsync(texts, cancellationToken);
            if (embeddings.Count != batch.Count)
            {
                _logger.LogWarning("Embedding batch boyutu eşleşmedi: {Expected} vs {Actual}", batch.Count, embeddings.Count);
                continue;
            }
            var toInsert = batch.Zip(embeddings, (c, e) => (c.path, c.content, e)).ToList();
            await _repo.InsertBatchAsync(toInsert, cancellationToken);
            total += toInsert.Count;
        }

        _logger.LogInformation("CodeChunkIndexService: Reindex tamamlandı. {Count} chunk yazıldı.", total);
        return total;
    }

    private void CollectChunksFromDir(string dirPath, string basePath, List<(string path, string content)> chunks)
    {
        foreach (var file in Directory.EnumerateFiles(dirPath, "*.*", SearchOption.AllDirectories))
        {
            if (chunks.Count >= MaxStructureFiles) return;
            var ext = Path.GetExtension(file);
            if (!AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase)) continue;
            var relative = Path.GetRelativePath(basePath, file);
            if (SkipDirs.Any(s => relative.Contains(Path.DirectorySeparatorChar + s + Path.DirectorySeparatorChar)
                || relative.StartsWith(s + Path.DirectorySeparatorChar, StringComparison.Ordinal)))
                continue;
            relative = relative.Replace('\\', '/');
            var content = ReadFileContent(file);
            if (!string.IsNullOrEmpty(content)) chunks.Add((relative, content));
        }
    }

    private static string ReadFileContent(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            return content.Length > MaxContentLength ? content[..MaxContentLength] + "\n... (kesildi)" : content;
        }
        catch
        {
            return string.Empty;
        }
    }
}
