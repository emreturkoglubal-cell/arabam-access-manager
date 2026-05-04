using AccessManager.Application.Interfaces;

namespace AccessManager.UI.Hosting;

/// <summary>İstanbul saati ile her gece 00:00 sonrası <see cref="IDailyMaintenanceRunner"/> çalıştırır.</summary>
public sealed class DailyMaintenanceHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyMaintenanceHostedService> _logger;

    public DailyMaintenanceHostedService(IServiceScopeFactory scopeFactory, ILogger<DailyMaintenanceHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = DelayUntilNextMidnightIstanbul();
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var runner = scope.ServiceProvider.GetRequiredService<IDailyMaintenanceRunner>();
                await runner.RunAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DailyMaintenanceHostedService: runner hatası.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    internal static TimeSpan DelayUntilNextMidnightIstanbul()
    {
        var tz = GetTurkeyTimeZone();
        var utcNow = DateTime.UtcNow;
        var trNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
        var nextMidnightTr = trNow.Date.AddDays(1);
        var nextMidnightUtc = TimeZoneInfo.ConvertTimeToUtc(nextMidnightTr, tz);
        var delay = nextMidnightUtc - utcNow;
        if (delay < TimeSpan.Zero)
            delay = TimeSpan.FromMinutes(1);
        return delay;
    }

    private static TimeZoneInfo GetTurkeyTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        }
        catch
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        }
    }
}
