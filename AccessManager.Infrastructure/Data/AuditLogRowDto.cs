using AccessManager.Domain.Enums;

namespace AccessManager.Infrastructure.Data;

/// <summary>Sayfalı audit log sorgusu sonucu (COUNT(*) OVER () ile TotalCount dahil).</summary>
internal sealed class AuditLogRowDto
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
    public long TotalCount { get; set; }
}
