namespace AccessManager.Application.Interfaces;

/// <summary>Günlük bakım (gece yarısı İstanbul): snapshot ve ileride diğer periyodik işler.</summary>
public interface IDailyMaintenanceRunner
{
    Task RunAsync(CancellationToken cancellationToken = default);
}
