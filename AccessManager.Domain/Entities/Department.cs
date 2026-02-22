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
}
