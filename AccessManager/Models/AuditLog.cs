namespace AccessManager.Models;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string TargetType { get; set; } = string.Empty; // Personnel, Access, Request...
    public string? TargetId { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
}
