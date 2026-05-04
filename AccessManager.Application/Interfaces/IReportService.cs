using AccessManager.Application.Dtos;

namespace AccessManager.Application.Interfaces;

/// <summary>
/// Raporlama: dashboard istatistikleri, sistem bazlı erişim, işten çıkış ve istisna raporları; Reports sayfası için toplu veri (GetReportsIndexData).
/// </summary>
public interface IReportService
{
    /// <summary>Dashboard için özet istatistikler (talep sayıları, personel, erişim vb.); departman ve dönem filtresi.</summary>
    /// <param name="departmentId">Faz 1: Departman bazlı filtre (null = tümü).</param>
    /// <param name="periodMonths">Son N ay (null = 1). 0 veya özel aralık için <paramref name="periodFrom"/>/<paramref name="periodTo"/> kullanılır.</param>
    /// <param name="periodFrom">Özel dönem başlangıcı (dahil).</param>
    /// <param name="periodTo">Özel dönem bitişi (dahil).</param>
    DashboardStats GetDashboardStats(int? departmentId = null, int? periodMonths = null, DateTime? periodFrom = null, DateTime? periodTo = null);

    /// <summary>Kontrol paneli grafikleri için veri (personel trendi, işten ayrılma, sistem/departman dağılımı).</summary>
    /// <param name="rangeFrom">Doluysa <paramref name="rangeTo"/> ile birlikte takvim ayları bu aralıkta üretilir (öncelikli).</param>
    /// <param name="rangeTo">Özel dönem bitiş ayı (dahil).</param>
    DashboardChartData GetDashboardChartData(int? departmentId = null, int periodMonths = 12, DateTime? rangeFrom = null, DateTime? rangeTo = null);
    /// <summary>Sistem bazlı aktif erişim sayıları (rapor satırları).</summary>
    IReadOnlyList<AccessBySystemReportRow> GetAccessReportBySystem();
    /// <summary>Belirtilen tarih aralığında işten çıkan personel raporu.</summary>
    IReadOnlyList<OffboardedReportRow> GetOffboardedReport(DateTime? from, DateTime? to, int? departmentId = null);
    /// <summary>İstisna olarak işaretlenmiş erişimlerin raporu.</summary>
    IReadOnlyList<ExceptionReportRow> GetExceptionReport();

    /// <summary>
    /// Raporlar sayfası için tüm veriyi tek turda çeker (performans için).
    /// </summary>
    ReportsIndexData GetReportsIndexData(DateTime? from, DateTime? to);

    /// <summary>Departman için aylık işe giren / işten çıkan sayıları (personel kayıtlarına göre).</summary>
    IReadOnlyList<DepartmentTurnoverPoint> GetDepartmentTurnoverPoints(int departmentId, int months = 12);

    /// <summary>Departmandaki aktif personellerin aktif uygulama yetkilerinin toplam USD maliyeti.</summary>
    decimal? GetDepartmentActiveLicenseCostUsd(int departmentId);
}
