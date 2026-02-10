using AccessManager.Application.Dtos;
using AccessManager.Application.Interfaces;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

[Authorize(Roles = AuthorizationRolePolicies.AdminManagerAuditor)]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet]
    public IActionResult Index(DateTime? from, DateTime? to)
    {
        var stats = _reportService.GetDashboardStats();
        var accessBySystem = _reportService.GetAccessReportBySystem() ?? new List<AccessBySystemReportRow>();
        var offboardedReport = _reportService.GetOffboardedReport(from, to) ?? new List<OffboardedReportRow>();
        var exceptionReport = _reportService.GetExceptionReport() ?? new List<ExceptionReportRow>();

        ViewBag.Stats = stats;
        ViewBag.AccessBySystem = accessBySystem;
        ViewBag.OffboardedReport = offboardedReport;
        ViewBag.ExceptionReport = exceptionReport;
        ViewBag.From = from;
        ViewBag.To = to;
        return View();
    }
}
