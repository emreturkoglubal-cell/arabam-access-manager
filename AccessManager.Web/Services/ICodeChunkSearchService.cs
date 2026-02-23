namespace AccessManager.UI.Services;

/// <summary>
/// Kullanıcı sorusuna en alakalı kod parçalarını vektör araması ile döner (RAG).
/// </summary>
public interface ICodeChunkSearchService
{
    /// <summary>Soru metnini embed edip en yakın K chunk'ı döner. Index yoksa boş liste.</summary>
    Task<IReadOnlyList<(string RepoPath, string Content)>> GetRelevantChunksAsync(string query, int topK = 10, CancellationToken cancellationToken = default);

    /// <summary>Vektör indexinde kayıt var mı.</summary>
    Task<bool> HasIndexAsync(CancellationToken cancellationToken = default);
}
