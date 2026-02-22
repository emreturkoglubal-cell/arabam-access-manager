namespace AccessManager.Domain.Enums;

/// <summary>
/// Kaynak sistemin (ResourceSystem) türü. Raporlama ve filtrelemede kullanılır.
/// </summary>
public enum SystemType
{
    /// <summary>Uygulama yazılımı (örn. CRM, ERP).</summary>
    Application,
    /// <summary>Altyapı / sunucu / ağ sistemi.</summary>
    Infrastructure,
    /// <summary>Lisans (yazılım lisansı vb.).</summary>
    License
}
