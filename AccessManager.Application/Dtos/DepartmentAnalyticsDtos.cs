namespace AccessManager.Application.Dtos;

public class DepartmentTurnoverPoint
{
    public string Label { get; set; } = string.Empty;
    public int Hires { get; set; }
    public int Exits { get; set; }
}
