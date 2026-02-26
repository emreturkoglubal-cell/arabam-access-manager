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
    [Display(Name = "Satın alma ücreti")]
    public decimal? PurchasePrice { get; set; }
    [Display(Name = "Para birimi")]
    public string PurchaseCurrency { get; set; } = "TRY";
    [DataType(DataType.Date)]
    public DateTime? DepreciationEndDate { get; set; }
}
