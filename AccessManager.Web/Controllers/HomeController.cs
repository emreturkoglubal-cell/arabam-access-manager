using System.Globalization;
using System.Text.Json;
using AccessManager.Application.Dtos;
using AccessManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace AccessManager.UI.Controllers;

/// <summary>
/// Ana sayfa ve hata sayfası. Dashboard istatistikleri (erişim sayıları, departman bazlı) gösterilir.
/// Stats ve grafik verisi MemoryCache ile önbelleğe alınır (TTL 10 dk).
/// </summary>
public class HomeController : Controller
{
    private readonly IReportService _reportService;
    private readonly IDepartmentService _departmentService;
    private readonly IMemoryCache _cache;

    private static readonly TimeSpan DashboardCacheDuration = TimeSpan.FromMinutes(10);

    public HomeController(IReportService reportService, IDepartmentService departmentService, IMemoryCache cache)
    {
        _reportService = reportService;
        _departmentService = departmentService;
        _cache = cache;
    }

    /// <summary>GET /Home/Index — Dashboard: departman ve dönem seçilerek erişim istatistikleri listelenir.</summary>
    [HttpGet]
    public IActionResult Index(int? departmentId, int? periodMonths, DateTime? customFrom, DateTime? customTo)
    {
        var rawPeriod = periodMonths ?? 1;
        DateTime? rangeFrom = null;
        DateTime? rangeTo = null;

        var useCustom = rawPeriod == 0 && customFrom.HasValue && customTo.HasValue;
        if (useCustom)
        {
            rangeFrom = customFrom!.Value.Date;
            rangeTo = customTo!.Value.Date;
            if (rangeFrom > rangeTo)
                (rangeFrom, rangeTo) = (rangeTo, rangeFrom);
        }

        // Özel dönem seçili ama tarih yoksa: veri son 1 ay; dropdown’da 0 kalır (tarih girme modu).
        var effectivePresetMonths = rawPeriod == 0
            ? 1
            : Math.Clamp(rawPeriod, 1, 120);

        var statsKey = useCustom
            ? $"Dashboard_Stats_{departmentId ?? 0}_c_{rangeFrom:yyyyMMdd}_{rangeTo:yyyyMMdd}"
            : $"Dashboard_Stats_{departmentId ?? 0}_{effectivePresetMonths}";

        var chartMonthsForPreset = Math.Clamp(effectivePresetMonths, 1, 24);
        var chartMonths = useCustom
            ? Math.Min(
                ((rangeTo!.Value.Year - rangeFrom!.Value.Year) * 12) + (rangeTo.Value.Month - rangeFrom.Value.Month) + 1,
                48)
            : chartMonthsForPreset;
        var chartsKey = useCustom
            ? $"Dashboard_Charts_{departmentId ?? 0}_c_{rangeFrom:yyyyMMdd}_{rangeTo:yyyyMMdd}"
            : $"Dashboard_Charts_{departmentId ?? 0}_{chartMonthsForPreset}";

        var stats = _cache.GetOrCreate(statsKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = DashboardCacheDuration;
            return useCustom
                ? _reportService.GetDashboardStats(departmentId, periodMonths: null, rangeFrom, rangeTo)
                : _reportService.GetDashboardStats(departmentId, effectivePresetMonths);
        });

        var chartData = _cache.GetOrCreate(chartsKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = DashboardCacheDuration;
            return useCustom
                ? _reportService.GetDashboardChartData(departmentId, periodMonths: chartMonths, rangeFrom: rangeFrom, rangeTo: rangeTo)
                : _reportService.GetDashboardChartData(departmentId, chartMonthsForPreset);
        });

        ViewBag.Departments = _departmentService.GetAll();
        ViewBag.DepartmentId = departmentId;
        ViewBag.PeriodMonths = rawPeriod == 0 ? 0 : effectivePresetMonths;
        ViewBag.CustomFrom = rawPeriod == 0 ? customFrom : null;
        ViewBag.CustomTo = rawPeriod == 0 ? customTo : null;
        ViewBag.UseCustomPeriod = useCustom;
        ViewBag.OffboardedPeriodLabel = useCustom
            ? $"{rangeFrom:dd.MM.yyyy} – {rangeTo:dd.MM.yyyy}"
            : effectivePresetMonths == 12
                ? "Son 1 yıl"
                : effectivePresetMonths == 24
                    ? "Son 2 yıl"
                    : $"Son {effectivePresetMonths} ay";
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        ViewBag.ChartMonthsLabel = useCustom
            ? $"{rangeFrom!.Value.ToString("MMM yyyy", tr)} – {rangeTo!.Value.ToString("MMM yyyy", tr)}"
            : $"{chartMonths} ay";

        ViewBag.ChartDataJson = JsonSerializer.Serialize(chartData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return View(stats);
    }

    /// <summary>GET /Home/Error — Hata sayfası (genel exception handler yönlendirmesi).</summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Error() => View();
}
