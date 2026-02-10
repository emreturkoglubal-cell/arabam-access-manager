namespace AccessManager.Models;

public class ResourceSystem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public SystemType SystemType { get; set; }
    public CriticalLevel CriticalLevel { get; set; }
    public Guid? OwnerId { get; set; }
    public string? Description { get; set; }

    public Personnel? Owner { get; set; }
}
