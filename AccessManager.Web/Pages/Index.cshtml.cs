using AccessManager.Application.Dtos;
using AccessManager.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.UI.Pages;

public class IndexModel : PageModel
{
    private readonly IReportService _reportService;

    public IndexModel(IReportService reportService)
    {
        _reportService = reportService;
    }

    public DashboardStats Stats { get; set; } = null!;

    public void OnGet()
    {
        Stats = _reportService.GetDashboardStats();
    }
}
