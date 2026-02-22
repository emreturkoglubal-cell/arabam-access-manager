using AccessManager.Application.Dtos;
using AccessManager.Application.Interfaces;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

/// <summary>
/// Raporlar: genel istatistikler, sistem bazlı erişim sayıları, offboard raporu, istisna (rol dışı erişim) raporu. Tarih aralığı (from, to) ile filtrelenebilir.
/// Yetki: Admin, Manager veya Auditor.
/// </summary>
[Authorize(Roles = AuthorizationRolePolicies.AdminManagerAuditor)]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>GET /Reports/Index — Rapor sayfası: istatistikler, sistem bazlı erişim, offboard ve istisna raporları; from/to ile dönem seçilir.</summary>
    [HttpGet]
    public IActionResult Index(DateTime? from, DateTime? to)
    {
        var data = _reportService.GetReportsIndexData(from, to);

        ViewBag.Stats = data.Stats;
        ViewBag.AccessBySystem = data.AccessBySystem;
        ViewBag.OffboardedReport = data.OffboardedReport;
        ViewBag.ExceptionReport = data.ExceptionReport;
        ViewBag.From = from;
        ViewBag.To = to;
        return View();
    }
}
