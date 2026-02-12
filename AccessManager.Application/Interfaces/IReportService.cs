using AccessManager.Application.Dtos;

namespace AccessManager.Application.Interfaces;

public interface IReportService
{
    /// <param name="departmentId">Faz 1: Departman bazlı filtre (null = tümü).</param>
    /// <param name="periodMonths">Faz 1: Son N ay (1 veya 3; null = 1).</param>
    DashboardStats GetDashboardStats(int? departmentId = null, int? periodMonths = null);
    IReadOnlyList<AccessBySystemReportRow> GetAccessReportBySystem();
    IReadOnlyList<OffboardedReportRow> GetOffboardedReport(DateTime? from, DateTime? to);
    IReadOnlyList<ExceptionReportRow> GetExceptionReport();

    /// <summary>
    /// Raporlar sayfası için tüm veriyi tek turda çeker (performans için).
    /// </summary>
    ReportsIndexData GetReportsIndexData(DateTime? from, DateTime? to);
}
