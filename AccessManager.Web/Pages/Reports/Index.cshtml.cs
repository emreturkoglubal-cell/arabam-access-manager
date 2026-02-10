using AccessManager.Application.Dtos;
using AccessManager.Application.Interfaces;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.UI.Pages.Reports;

[Authorize(Roles = AuthorizationRolePolicies.AdminManagerAuditor)]
public class IndexModel : PageModel
{
    private readonly IReportService _reportService;

    public IndexModel(IReportService reportService)
    {
        _reportService = reportService;
    }

    public DashboardStats Stats { get; set; } = null!;
    public IReadOnlyList<AccessBySystemReportRow> AccessBySystem { get; set; } = new List<AccessBySystemReportRow>();
    public IReadOnlyList<OffboardedReportRow> OffboardedReport { get; set; } = new List<OffboardedReportRow>();
    public IReadOnlyList<ExceptionReportRow> ExceptionReport { get; set; } = new List<ExceptionReportRow>();

    public void OnGet(DateTime? from, DateTime? to)
    {
        Stats = _reportService.GetDashboardStats();
        AccessBySystem = _reportService.GetAccessReportBySystem() ?? new List<AccessBySystemReportRow>();
        OffboardedReport = _reportService.GetOffboardedReport(from, to) ?? new List<OffboardedReportRow>();
        ExceptionReport = _reportService.GetExceptionReport() ?? new List<ExceptionReportRow>();
    }
}
