namespace AccessManager.UI.ViewModels;

public class PersonnelCreateInputModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int DepartmentId { get; set; }
    public string? Position { get; set; }
    public int? ManagerId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public int? RoleId { get; set; }
    /// <summary>Seçiliyse personel managers tablosuna level 4 ile eklenir.</summary>
    public bool IsManager { get; set; }
}
