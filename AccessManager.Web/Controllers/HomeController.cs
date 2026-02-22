using AccessManager.Application.Dtos;
using AccessManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

/// <summary>
/// Ana sayfa ve hata sayfası. Dashboard istatistikleri (erişim sayıları, departman bazlı) gösterilir.
/// </summary>
public class HomeController : Controller
{
    private readonly IReportService _reportService;
    private readonly IDepartmentService _departmentService;

    public HomeController(IReportService reportService, IDepartmentService departmentService)
    {
        _reportService = reportService;
        _departmentService = departmentService;
    }

    /// <summary>GET /Home/Index — Dashboard: departman ve dönem seçilerek erişim istatistikleri listelenir.</summary>
    [HttpGet]
    public IActionResult Index(int? departmentId, int? periodMonths)
    {
        var stats = _reportService.GetDashboardStats(departmentId, periodMonths);
        ViewBag.Departments = _departmentService.GetAll();
        ViewBag.DepartmentId = departmentId;
        ViewBag.PeriodMonths = periodMonths ?? 1;
        return View(stats);
    }

    /// <summary>GET /Home/Error — Hata sayfası (genel exception handler yönlendirmesi).</summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Error() => View();
}
