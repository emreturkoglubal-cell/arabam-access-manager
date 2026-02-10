using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.Infrastructure.Data;

namespace AccessManager.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly MockDataStore _store;

    public AuditService(MockDataStore store)
    {
        _store = store;
    }

    public void Log(AuditAction action, Guid? actorId, string actorName, string targetType, string? targetId, string? details = null, string? ipAddress = null)
    {
        _store.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            ActorName = actorName,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Details = details,
            Timestamp = DateTime.UtcNow,
            IpAddress = ipAddress
        });
    }

    public IReadOnlyList<AuditLog> GetRecent(int count = 100) =>
        _store.AuditLogs.OrderByDescending(l => l.Timestamp).Take(count).ToList();

    public IReadOnlyList<AuditLog> GetByTarget(string targetType, string? targetId = null)
    {
        var q = _store.AuditLogs.Where(l => l.TargetType == targetType);
        if (targetId != null) q = q.Where(l => l.TargetId == targetId);
        return q.OrderByDescending(l => l.Timestamp).ToList();
    }

    public IReadOnlyList<AuditLog> GetByDateRange(DateTime from, DateTime to) =>
        _store.AuditLogs.Where(l => l.Timestamp >= from && l.Timestamp <= to).OrderByDescending(l => l.Timestamp).ToList();
}
