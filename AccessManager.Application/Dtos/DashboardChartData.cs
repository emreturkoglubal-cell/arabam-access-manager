namespace AccessManager.Application.Dtos;

/// <summary>Kontrol paneli grafikleri için veri.</summary>
public class DashboardChartData
{
    /// <summary>Son N ay: her ay sonu aktif personel sayısı (ay etiketi, sayı).</summary>
    public List<MonthCountPair> PersonnelTrend { get; set; } = new();

    /// <summary>Son N ay: aylık işten ayrılan sayısı.</summary>
    public List<MonthCountPair> OffboardedByMonth { get; set; } = new();

    /// <summary>Uygulamalara göre aktif erişim sayısı (en çok 10).</summary>
    public List<LabelCountPair> AccessBySystem { get; set; } = new();

    /// <summary>Departmanlara göre aktif personel sayısı.</summary>
    public List<LabelCountPair> PersonnelByDepartment { get; set; } = new();
}

public class MonthCountPair
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class LabelCountPair
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}
