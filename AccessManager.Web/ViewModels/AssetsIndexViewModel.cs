using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.UI.ViewModels;

/// <summary>Donanım & Zimmet listesi; veriler veritabanından (IAssetService) doldurulur.</summary>
public class AssetsIndexViewModel
{
    public IReadOnlyList<Asset> Assets { get; set; } = new List<Asset>();
    public Dictionary<int, AssetAssignment> AssignmentByAsset { get; set; } = new();
    public Dictionary<int, string> PersonNames { get; set; } = new();
    public AssetStatus? FilterStatus { get; set; }
    public AssetType? FilterType { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
