using AccessManager.Data;
using AccessManager.Models;

namespace AccessManager.Services;

public class AuditService : IAuditService
{
    private readonly MockDataStore _store = MockDataStore.Current;

    public void Log(AuditAction action, Guid? actorId, string actorName, string targetType, string? targetId, string? details = null)
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
            Timestamp = DateTime.UtcNow
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
