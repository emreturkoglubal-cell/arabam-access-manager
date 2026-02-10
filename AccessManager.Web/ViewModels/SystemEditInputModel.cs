using AccessManager.Domain.Enums;

namespace AccessManager.UI.ViewModels;

public class SystemEditInputModel
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public SystemType SystemType { get; set; } = SystemType.Application;
    public CriticalLevel CriticalLevel { get; set; } = CriticalLevel.Medium;
    public int OwnerId { get; set; }
    public string? Description { get; set; }
}
