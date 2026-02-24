namespace AccessManager.UI.ViewModels;

/// <summary>
/// Personel detay sayfasında kişisel bilgiler modalından gelen düzenleme verisi. ID değiştirilemez.
/// </summary>
public class PersonnelEditInputModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string? Position { get; set; }
    public int? ManagerId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? RoleId { get; set; }
    /// <summary>0=Active, 1=Passive, 2=Offboarded</summary>
    public short Status { get; set; }
}
