namespace AccessManager.Domain.Entities;

/// <summary>
/// Departman. Personeller departmana bağlıdır; raporlama ve filtrelemede kullanılır.
/// </summary>
public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    /// <summary>Üst departman (alt kırılım).</summary>
    public int? ParentId { get; set; }
    /// <summary>Departman en üst yöneticisi (GMY/Direktör).</summary>
    public int? TopManagerPersonnelId { get; set; }

    public Department? Parent { get; set; }
    public Personnel? TopManager { get; set; }
}
