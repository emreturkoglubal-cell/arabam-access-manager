using AccessManager.Domain.Entities;

namespace AccessManager.Infrastructure.Repositories;

public interface IAuditLogRepository
{
    void Insert(AuditLog log);
    IReadOnlyList<AuditLog> GetRecent(int count);
    IReadOnlyList<AuditLog> GetByTarget(string targetType, string? targetId);
    IReadOnlyList<AuditLog> GetByDateRange(DateTime from, DateTime to);
    (IReadOnlyList<AuditLog> Items, int TotalCount) GetPaged(string? targetType, int page, int pageSize);
}
