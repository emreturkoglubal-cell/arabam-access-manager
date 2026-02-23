namespace AccessManager.Infrastructure.Repositories;

/// <summary>
/// Kod parçası embedding'leri (pgvector). Toplu insert, tümünü silme, benzerlik araması.
/// </summary>
public interface ICodeChunkRepository
{
    /// <summary>Tüm code_chunks kayıtlarını siler (reindex öncesi).</summary>
    Task DeleteAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Birden fazla chunk ekler (path, content, embedding).</summary>
    Task InsertBatchAsync(IReadOnlyList<(string RepoPath, string Content, float[] Embedding)> chunks, CancellationToken cancellationToken = default);

    /// <summary>Soru embedding'ine en yakın K kaydı döner (cosine benzerliği).</summary>
    Task<IReadOnlyList<(string RepoPath, string Content)>> GetNearestAsync(float[] queryEmbedding, int limit, CancellationToken cancellationToken = default);

    /// <summary>Tablo boş mu (index var mı kontrolü).</summary>
    Task<bool> HasAnyAsync(CancellationToken cancellationToken = default);
}
