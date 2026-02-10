using AccessManager.Domain.Enums;

namespace AccessManager.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public int? ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
}
