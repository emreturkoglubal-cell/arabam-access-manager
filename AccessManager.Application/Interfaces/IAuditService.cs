using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.Application.Interfaces;

public interface IAuditService
{
    void Log(AuditAction action, Guid? actorId, string actorName, string targetType, string? targetId, string? details = null, string? ipAddress = null);
    IReadOnlyList<AuditLog> GetRecent(int count = 100);
    IReadOnlyList<AuditLog> GetByTarget(string targetType, string? targetId = null);
    IReadOnlyList<AuditLog> GetByDateRange(DateTime from, DateTime to);
}
