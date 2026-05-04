namespace AccessManager.UI.ViewModels;

public class DepartmentCreateInputModel
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public int? TopManagerPersonnelId { get; set; }
}
