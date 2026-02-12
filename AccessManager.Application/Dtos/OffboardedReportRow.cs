namespace AccessManager.Application.Dtos;

public class OffboardedReportRow
{
    public int PersonnelId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime? EndDate { get; set; }
    public string? Department { get; set; }
}
