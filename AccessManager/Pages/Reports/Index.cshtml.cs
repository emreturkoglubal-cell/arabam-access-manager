using AccessManager.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.Pages.Reports;

public class IndexModel : PageModel
{
    private readonly IReportService _reportService;

    public IndexModel(IReportService reportService)
    {
        _reportService = reportService;
    }

    public DashboardStats Stats { get; set; } = null!;
    public IReadOnlyList<object> AccessBySystem { get; set; } = new List<object>();
    public IReadOnlyList<object> OffboardedReport { get; set; } = new List<object>();
    public IReadOnlyList<object> ExceptionReport { get; set; } = new List<object>();

    public void OnGet(DateTime? from, DateTime? to)
    {
        Stats = _reportService.GetDashboardStats();
        AccessBySystem = _reportService.GetAccessReportBySystem();
        OffboardedReport = _reportService.GetOffboardedReport(from, to);
        ExceptionReport = _reportService.GetExceptionReport();
    }
}
