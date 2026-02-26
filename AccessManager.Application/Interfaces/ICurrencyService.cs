namespace AccessManager.Application.Interfaces;

/// <summary>
/// Para birimi kurları (baz: USD). MemoryCache ile önbelleğe alınır.
/// </summary>
public interface ICurrencyService
{
    /// <summary>Para birimi kodu -> 1 birim = kaç USD. Örn. TRY -> 0.0228, USD -> 1, EUR -> 1.18.</summary>
    IReadOnlyDictionary<string, decimal> GetRatesToUsd();
}
