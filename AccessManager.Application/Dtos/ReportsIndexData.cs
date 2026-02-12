namespace AccessManager.Application.Dtos;

/// <summary>
/// Raporlar sayfası için tek seferde yüklenen tüm veri (N+1 / tekrarlı sorgu önlemi).
/// </summary>
public class ReportsIndexData
{
    public DashboardStats Stats { get; set; } = new();
    public IReadOnlyList<AccessBySystemReportRow> AccessBySystem { get; set; } = new List<AccessBySystemReportRow>();
    public IReadOnlyList<OffboardedReportRow> OffboardedReport { get; set; } = new List<OffboardedReportRow>();
    public IReadOnlyList<ExceptionReportRow> ExceptionReport { get; set; } = new List<ExceptionReportRow>();
}
