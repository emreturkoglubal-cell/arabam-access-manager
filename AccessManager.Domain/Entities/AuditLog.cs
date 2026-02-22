using AccessManager.Domain.Enums;

namespace AccessManager.Domain.Entities;

/// <summary>
/// Denetim günlüğü kaydı: kim (ActorId/ActorName), ne işlem (AuditAction), hangi hedef (TargetType/TargetId), detay ve zaman; isteğe bağlı IP. Tüm önemli işlemler burada loglanır.
/// </summary>
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
