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
