using AccessManager.Application.Dtos;

namespace AccessManager.Application.Interfaces;

/// <summary>
/// Raporlama: dashboard istatistikleri, sistem bazlı erişim, işten çıkış ve istisna raporları; Reports sayfası için toplu veri (GetReportsIndexData).
/// </summary>
public interface IReportService
{
    /// <summary>Dashboard için özet istatistikler (talep sayıları, personel, erişim vb.); departman ve dönem filtresi.</summary>
    /// <param name="departmentId">Faz 1: Departman bazlı filtre (null = tümü).</param>
    /// <param name="periodMonths">Faz 1: Son N ay (1 veya 3; null = 1).</param>
    DashboardStats GetDashboardStats(int? departmentId = null, int? periodMonths = null);
    /// <summary>Sistem bazlı aktif erişim sayıları (rapor satırları).</summary>
    IReadOnlyList<AccessBySystemReportRow> GetAccessReportBySystem();
    /// <summary>Belirtilen tarih aralığında işten çıkan personel raporu.</summary>
    IReadOnlyList<OffboardedReportRow> GetOffboardedReport(DateTime? from, DateTime? to);
    /// <summary>İstisna olarak işaretlenmiş erişimlerin raporu.</summary>
    IReadOnlyList<ExceptionReportRow> GetExceptionReport();

    /// <summary>
    /// Raporlar sayfası için tüm veriyi tek turda çeker (performans için).
    /// </summary>
    ReportsIndexData GetReportsIndexData(DateTime? from, DateTime? to);
}
