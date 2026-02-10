using AccessManager.Application.Dtos;
using AccessManager.Application.Interfaces;
using AccessManager.Domain.Constants;
using AccessManager.Domain.Enums;
using AccessManager.Infrastructure.Data;

namespace AccessManager.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly MockDataStore _store;

    public ReportService(MockDataStore store)
    {
        _store = store;
    }

    public DashboardStats GetDashboardStats(Guid? departmentId = null, int? periodMonths = null)
    {
        var now = DateTime.UtcNow;
        var months = periodMonths.HasValue && periodMonths.Value > 0 ? Math.Min(periodMonths.Value, 120) : 1;
        var periodStart = now.AddMonths(-months).Date;

        var personnelInScope = departmentId.HasValue
            ? _store.Personnel.Where(p => p.DepartmentId == departmentId.Value).Select(p => p.Id).ToHashSet()
            : null;

        bool InScope(Guid pid) => personnelInScope == null || personnelInScope.Contains(pid);

        var activePersonnel = _store.Personnel.Where(p => p.Status == PersonnelStatus.Active && (personnelInScope == null || personnelInScope.Contains(p.Id))).ToList();
        var offboardedInPeriod = _store.Personnel.Count(p => p.EndDate.HasValue && p.EndDate >= periodStart && (personnelInScope == null || personnelInScope.Contains(p.Id)));

        var pendingRequests = _store.AccessRequests.Where(r =>
            r.Status == AccessRequestStatus.PendingManager || r.Status == AccessRequestStatus.PendingSystemOwner || r.Status == AccessRequestStatus.PendingIT);
        var pendingCount = personnelInScope == null
            ? pendingRequests.Count()
            : pendingRequests.Count(r => personnelInScope.Contains(r.PersonnelId));

        return new DashboardStats
        {
            ActivePersonnelCount = activePersonnel.Count,
            OffboardedLastMonthCount = offboardedInPeriod,
            PendingRequestsCount = pendingCount,
            ExpiringAccessCount = _store.PersonnelAccesses.Count(a => a.IsActive && a.ExpiresAt.HasValue && a.ExpiresAt <= now.AddDays(30) && InScope(a.PersonnelId)),
            ExceptionAccessCount = _store.PersonnelAccesses.Count(a => a.IsActive && a.IsException && InScope(a.PersonnelId))
        };
    }

    public IReadOnlyList<AccessBySystemReportRow> GetAccessReportBySystem()
    {
        if (_store.ResourceSystems.Count == 0)
            return new List<AccessBySystemReportRow>();

        return _store.ResourceSystems.Select(s => new AccessBySystemReportRow
        {
            SystemName = s.Name ?? string.Empty,
            SystemCode = s.Code ?? string.Empty,
            ActiveAccessCount = _store.PersonnelAccesses.Count(a => a.ResourceSystemId == s.Id && a.IsActive)
        }).ToList();
    }

    public IReadOnlyList<OffboardedReportRow> GetOffboardedReport(DateTime? from, DateTime? to)
    {
        var list = _store.Personnel.Where(p => p.EndDate.HasValue).AsEnumerable();
        if (from.HasValue) list = list.Where(p => p.EndDate >= from);
        if (to.HasValue) list = list.Where(p => p.EndDate <= to);
        return list.Select(p => new OffboardedReportRow
        {
            PersonnelId = p.Id,
            SicilNo = p.SicilNo ?? string.Empty,
            FullName = $"{p.FirstName} {p.LastName}".Trim(),
            EndDate = p.EndDate,
            Department = _store.Departments.FirstOrDefault(d => d.Id == p.DepartmentId)?.Name
        }).ToList();
    }

    public IReadOnlyList<ExceptionReportRow> GetExceptionReport()
    {
        var query = _store.PersonnelAccesses
            .Where(a => a.IsActive && a.IsException)
            .Join(_store.Personnel, a => a.PersonnelId, p => p.Id, (a, p) => new { a, p })
            .Join(_store.ResourceSystems, x => x.a.ResourceSystemId, s => s.Id, (x, s) => new ExceptionReportRow
            {
                PersonnelId = x.p.Id,
                Person = $"{x.p.FirstName} {x.p.LastName}".Trim(),
                SicilNo = x.p.SicilNo ?? string.Empty,
                System = s.Name ?? string.Empty,
                Permission = PermissionTypeLabels.Get(x.a.PermissionType),
                ExpiresAt = x.a.ExpiresAt
            });
        return query.ToList();
    }
}
