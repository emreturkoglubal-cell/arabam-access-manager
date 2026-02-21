using Microsoft.Extensions.Logging;

namespace AccessManager.UI.Logging;

/// <summary>
/// ExtendedLogLogger üreten provider. ILogger&lt;T&gt; kullanıldığında Error/Critical loglar extended_logs tablosuna gider.
/// </summary>
public sealed class ExtendedLogLoggerProvider : ILoggerProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ExtendedLogLoggerProvider(IServiceScopeFactory scopeFactory, IHttpContextAccessor httpContextAccessor)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public ILogger CreateLogger(string categoryName)
        => new ExtendedLogLogger(categoryName, _scopeFactory, _httpContextAccessor);

    public void Dispose() { }
}
