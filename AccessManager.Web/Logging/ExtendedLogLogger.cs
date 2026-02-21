using AccessManager.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using AccessManager.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace AccessManager.UI.Logging;

/// <summary>
/// ILogger uygulaması: Error/Critical seviyesinde extended_logs tablosuna yazar (IP, URL, user agent vb.).
/// </summary>
public sealed class ExtendedLogLogger : ILogger
{
    private readonly string _categoryName;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ExtendedLogLogger(string categoryName, IServiceScopeFactory scopeFactory, IHttpContextAccessor httpContextAccessor)
    {
        _categoryName = categoryName ?? nameof(ExtendedLogLogger);
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel)
        => logLevel is LogLevel.Error or LogLevel.Critical;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel) || formatter == null)
            return;

        try
        {
            var message = formatter(state, exception);
            var httpContext = _httpContextAccessor.HttpContext;

            var log = new ExtendedLog
            {
                Level = logLevel == LogLevel.Critical ? "Error" : "Error",
                Source = _categoryName,
                Message = message.Length > 8000 ? message[..8000] + "…" : message,
                Exception = exception != null ? (exception.ToString().Length > 16000 ? exception.ToString()[..16000] + "…" : exception.ToString()) : null,
                CreatedAt = DateTime.UtcNow,
                ExtraData = eventId.Id != 0 ? $"EventId:{eventId.Id}" : null
            };

            if (httpContext != null)
            {
                log.IpAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                log.Url = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.Path}{httpContext.Request.QueryString}";
                log.HttpMethod = httpContext.Request.Method;
                var ua = httpContext.Request.Headers.UserAgent.FirstOrDefault();
                log.UserAgent = ua != null && ua.Length > 500 ? ua[..500] : ua;

                var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out var userId))
                    log.UserId = userId;
                log.UserName = httpContext.User.Identity?.Name
                    ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value;
            }

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetService<IExtendedLogRepository>();
            repo?.Insert(log);
        }
        catch
        {
            // Log yazarken hata olursa uygulama davranışını bozmayalım
        }
    }
}
