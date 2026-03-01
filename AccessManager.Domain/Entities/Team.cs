namespace AccessManager.Domain.Entities;

/// <summary>Departman alt ekibi (örn. Bilgi Teknolojileri -> DevOps, Development).</summary>
public class Team
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public DateTime CreatedAt { get; set; }

    public Department? Department { get; set; }
}
