using AccessManager.Domain.Enums;

namespace AccessManager.Domain.Entities;

/// <summary>Donanım / varlık (bilgisayar, telefon vb.).</summary>
public class Asset
{
    public int Id { get; set; }
    public AssetType AssetType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? BrandModel { get; set; }
    public AssetStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? PurchaseDate { get; set; }
    /// <summary>Satın alınma ücreti.</summary>
    public decimal? PurchasePrice { get; set; }
    /// <summary>Satın alma ücreti para birimi: TRY, USD, EUR.</summary>
    public string? PurchaseCurrency { get; set; }
    /// <summary>Amortisman bitiş tarihi; boşsa satın alma + 5 yıl kabul edilir.</summary>
    public DateTime? DepreciationEndDate { get; set; }
    /// <summary>Amortisman süresi (yıl); 1-5.</summary>
    public short? DepreciationYears { get; set; }
    /// <summary>RAM (GB) - Laptop/Phone/Tablet.</summary>
    public int? SpecRamGb { get; set; }
    /// <summary>Depolama (GB) - Laptop/Phone/Tablet.</summary>
    public int? SpecStorageGb { get; set; }
    /// <summary>İşlemci - Laptop.</summary>
    public string? SpecCpu { get; set; }
    /// <summary>Ekran boyutu (inç).</summary>
    public decimal? SpecScreenInches { get; set; }
    /// <summary>Pivot (döndürülebilir) - Monitör.</summary>
    public bool? SpecIsPivot { get; set; }
    public DateTime CreatedAt { get; set; }
}
