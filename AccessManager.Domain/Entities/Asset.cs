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
    public DateTime CreatedAt { get; set; }
}
