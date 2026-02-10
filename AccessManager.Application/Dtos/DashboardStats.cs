namespace AccessManager.Application.Dtos;

public class DashboardStats
{
    public int ActivePersonnelCount { get; set; }
    public int OffboardedLastMonthCount { get; set; }
    public int PendingRequestsCount { get; set; }
    public int ExpiringAccessCount { get; set; }
    public int ExceptionAccessCount { get; set; }
}
