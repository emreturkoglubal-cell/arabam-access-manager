namespace AccessManager.Application.Dtos;

public class ExceptionReportRow
{
    public Guid PersonnelId { get; set; }
    public string Person { get; set; } = string.Empty;
    public string SicilNo { get; set; } = string.Empty;
    public string System { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}
