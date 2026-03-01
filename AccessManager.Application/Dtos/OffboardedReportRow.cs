namespace AccessManager.Application.Dtos;

public class OffboardedReportRow
{
    public int PersonnelId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Department { get; set; }
    public bool HasOpenAccess { get; set; }
}
