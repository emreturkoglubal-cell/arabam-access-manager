namespace AccessManager.Services;

public class DashboardStats
{
    public int ActivePersonnelCount { get; set; }
    public int OffboardedLastMonthCount { get; set; }
    public int PendingRequestsCount { get; set; }
    public int ExpiringAccessCount { get; set; }
    public int ExceptionAccessCount { get; set; }
}

public interface IReportService
{
    DashboardStats GetDashboardStats();
    IReadOnlyList<object> GetAccessReportBySystem();
    IReadOnlyList<object> GetOffboardedReport(DateTime? from, DateTime? to);
    IReadOnlyList<object> GetExceptionReport();
}
