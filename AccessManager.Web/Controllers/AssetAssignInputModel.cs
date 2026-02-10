namespace AccessManager.UI.Controllers;

public class AssetAssignInputModel
{
    public Guid AssetId { get; set; }
    public Guid PersonnelId { get; set; }
    public string? Notes { get; set; }
}
