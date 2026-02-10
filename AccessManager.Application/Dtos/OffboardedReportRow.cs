namespace AccessManager.Application.Dtos;

public class OffboardedReportRow
{
    public Guid PersonnelId { get; set; }
    public string SicilNo { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime? EndDate { get; set; }
    public string? Department { get; set; }
}
