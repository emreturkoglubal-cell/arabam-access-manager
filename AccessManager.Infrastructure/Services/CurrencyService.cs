using AccessManager.Application.Interfaces;
using AccessManager.Infrastructure.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace AccessManager.Infrastructure.Services;

public class CurrencyService : ICurrencyService
{
    private const string CacheKey = "CurrencyRates_ToUsd";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private readonly ICurrencyRateRepository _repo;
    private readonly IMemoryCache _cache;

    public CurrencyService(ICurrencyRateRepository repo, IMemoryCache cache)
    {
        _repo = repo;
        _cache = cache;
    }

    public IReadOnlyDictionary<string, decimal> GetRatesToUsd()
    {
        return _cache.GetOrCreate(CacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var list = _repo.GetAll();
            var dict = list.ToDictionary(c => c.Code.Trim().ToUpperInvariant(), c => c.RateToUsd, StringComparer.OrdinalIgnoreCase);
            return (IReadOnlyDictionary<string, decimal>)dict;
        }) ?? new Dictionary<string, decimal>();
    }
}
