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
    public IActionResult Index(int? departmentId, int? periodMonths)
    {
        var period = periodMonths ?? 1;
        var statsKey = $"Dashboard_Stats_{departmentId ?? 0}_{period}";
        var chartsKey = $"Dashboard_Charts_{departmentId ?? 0}";

        var stats = _cache.GetOrCreate(statsKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = DashboardCacheDuration;
            return _reportService.GetDashboardStats(departmentId, periodMonths);
        });

        var chartData = _cache.GetOrCreate(chartsKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = DashboardCacheDuration;
            return _reportService.GetDashboardChartData(departmentId, periodMonths: 12);
        });

        ViewBag.Departments = _departmentService.GetAll();
        ViewBag.DepartmentId = departmentId;
        ViewBag.PeriodMonths = period;
        ViewBag.ChartDataJson = JsonSerializer.Serialize(chartData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return View(stats);
    }

    /// <summary>GET /Home/Error — Hata sayfası (genel exception handler yönlendirmesi).</summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Error() => View();
}
