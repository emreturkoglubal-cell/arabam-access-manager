namespace AccessManager.UI.Controllers;

public class AssetReturnInputModel
{
    public Guid AssignmentId { get; set; }
    public string? ReturnCondition { get; set; }
    public string? Notes { get; set; }
}
