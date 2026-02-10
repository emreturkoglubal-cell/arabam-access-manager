namespace AccessManager.UI.ViewModels;

public class PersonnelCreateInputModel
{
    public string SicilNo { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string? Position { get; set; }
    public int? ManagerId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public int? RoleId { get; set; }
}
