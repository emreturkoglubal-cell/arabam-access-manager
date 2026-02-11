using AccessManager.Application;
using AccessManager.Application.Dtos;
using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.Infrastructure.Repositories;

namespace AccessManager.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _repo;

    public AuditService(IAuditLogRepository repo)
    {
        _repo = repo;
    }

    public void Log(AuditAction action, int? actorId, string actorName, string targetType, string? targetId, string? details = null, string? ipAddress = null)
    {
        _repo.Insert(new AuditLog
        {
            ActorId = actorId,
            ActorName = actorName,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Details = details,
            Timestamp = SystemTime.Now,
            IpAddress = ipAddress
        });
    }

    public IReadOnlyList<AuditLog> GetRecent(int count = 100) => _repo.GetRecent(count);

    public IReadOnlyList<AuditLog> GetByTarget(string targetType, string? targetId = null) => _repo.GetByTarget(targetType, targetId);

    public IReadOnlyList<AuditLog> GetByDateRange(DateTime from, DateTime to) => _repo.GetByDateRange(from, to);

    public PagedResult<AuditLog> GetPaged(string? targetType, int page, int pageSize)
    {
        var (items, totalCount) = _repo.GetPaged(targetType, page, pageSize);
        return new PagedResult<AuditLog>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }
}
