using System.ComponentModel.DataAnnotations;
using AccessManager.Domain.Enums;

namespace AccessManager.UI.ViewModels;

public class AssetEditInputModel
{
    public AssetType AssetType { get; set; } = AssetType.Laptop;
    [Required(ErrorMessage = "Ad gerekli")]
    public string Name { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? BrandModel { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.Available;
    public string? Notes { get; set; }
    [DataType(DataType.Date)]
    public DateTime? PurchaseDate { get; set; }
    /// <summary>Satın alınma ücreti (TL).</summary>
    [Display(Name = "Satın alma ücreti (TL)")]
    public decimal? PurchasePrice { get; set; }
    [DataType(DataType.Date)]
    public DateTime? DepreciationEndDate { get; set; }
}
