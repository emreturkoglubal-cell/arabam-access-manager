namespace AccessManager.Domain.Entities;

public class PositionTitleTemplate
{
    public int Id { get; set; }
    public int? DepartmentId { get; set; }
    public int? TeamId { get; set; }
    public string? SeniorityLevel { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
