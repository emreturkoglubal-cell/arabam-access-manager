namespace AccessManager.Application;

/// <summary>Uygulama saati: Türkiye (UTC+3). DB'ye yazılan tüm "şimdi" değerleri buradan kullanılır.</summary>
public static class SystemTime
{
    private static readonly TimeZoneInfo Turkey = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "Turkey" : "Europe/Istanbul");

    /// <summary>Türkiye saati (UTC+3) ile şu an.</summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Turkey);
}
