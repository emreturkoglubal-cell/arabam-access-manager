namespace AccessManager.UI.Services;

/// <summary>
/// Proje kaynak kodu metnini LLM bağlamı için toplar (Faz 1: sadece okuma).
/// </summary>
public interface ICodeContextService
{
    /// <summary>
    /// Yapılandırılmış kaynak kodu metnini döner (dosya yolu + içerik). Karakter limiti config'den okunur.
    /// </summary>
    Task<string> GetCodeContextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Proje yapısı özeti: repo köküne göre önemli dosya yolları. Model read_file ile detay çeker.
    /// </summary>
    Task<string> GetProjectStructureAsync(CancellationToken cancellationToken = default);
}
