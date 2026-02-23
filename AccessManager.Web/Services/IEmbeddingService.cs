namespace AccessManager.UI.Services;

/// <summary>
/// Metin için embedding vektörü üretir (OpenAI Embeddings API). RAG index ve soru araması için kullanılır.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>Tek metin için 1536 boyutlu embedding döner (text-embedding-3-small).</summary>
    Task<float[]?> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>Birden fazla metin için embedding'ler; batch istek ile (rate limit için daha verimli).</summary>
    Task<IReadOnlyList<float[]>> GetEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
}
