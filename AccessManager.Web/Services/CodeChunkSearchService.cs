using AccessManager.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace AccessManager.UI.Services;

public class CodeChunkSearchService : ICodeChunkSearchService
{
    private readonly ICodeChunkRepository _repo;
    private readonly IEmbeddingService _embedding;
    private readonly ILogger<CodeChunkSearchService> _logger;

    public CodeChunkSearchService(ICodeChunkRepository repo, IEmbeddingService embedding, ILogger<CodeChunkSearchService> logger)
    {
        _repo = repo;
        _embedding = embedding;
        _logger = logger;
    }

    public async Task<IReadOnlyList<(string RepoPath, string Content)>> GetRelevantChunksAsync(string query, int topK = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || topK < 1)
            return Array.Empty<(string, string)>();

        var hasIndex = await _repo.HasAnyAsync(cancellationToken);
        if (!hasIndex)
            return Array.Empty<(string, string)>();

        var embedding = await _embedding.GetEmbeddingAsync(query.Trim(), cancellationToken);
        if (embedding == null || embedding.Length == 0)
        {
            _logger.LogWarning("CodeChunkSearchService: Soru için embedding alınamadı.");
            return Array.Empty<(string, string)>();
        }

        return await _repo.GetNearestAsync(embedding, topK, cancellationToken);
    }

    public Task<bool> HasIndexAsync(CancellationToken cancellationToken = default) => _repo.HasAnyAsync(cancellationToken);
}
