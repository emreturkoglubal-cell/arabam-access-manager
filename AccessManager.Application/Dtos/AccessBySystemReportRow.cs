namespace AccessManager.Application.Dtos;

public class AccessBySystemReportRow
{
    public string SystemName { get; set; } = string.Empty;
    public string SystemCode { get; set; } = string.Empty;
    public int ActiveAccessCount { get; set; }
}
