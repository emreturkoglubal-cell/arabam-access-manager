using AccessManager.Data;
using AccessManager.Models;

namespace AccessManager.Services;

public class ReportService : IReportService
{
    private readonly MockDataStore _store = MockDataStore.Current;

    public DashboardStats GetDashboardStats()
    {
        var now = DateTime.UtcNow;
        var lastMonthStart = now.AddMonths(-1).Date;
        return new DashboardStats
        {
            ActivePersonnelCount = _store.Personnel.Count(p => p.Status == PersonnelStatus.Active),
            OffboardedLastMonthCount = _store.Personnel.Count(p => p.EndDate.HasValue && p.EndDate >= lastMonthStart),
            PendingRequestsCount = _store.AccessRequests.Count(r => r.Status == AccessRequestStatus.PendingManager || r.Status == AccessRequestStatus.PendingSystemOwner || r.Status == AccessRequestStatus.PendingIT),
            ExpiringAccessCount = _store.PersonnelAccesses.Count(a => a.IsActive && a.ExpiresAt.HasValue && a.ExpiresAt <= now.AddDays(30)),
            ExceptionAccessCount = _store.PersonnelAccesses.Count(a => a.IsActive && a.IsException)
        };
    }

    public IReadOnlyList<object> GetAccessReportBySystem()
    {
        return _store.ResourceSystems.Select(s => (object)new
        {
            SystemName = s.Name,
            SystemCode = s.Code,
            ActiveAccessCount = _store.PersonnelAccesses.Count(a => a.ResourceSystemId == s.Id && a.IsActive)
        }).ToList();
    }

    public IReadOnlyList<object> GetOffboardedReport(DateTime? from, DateTime? to)
    {
        var list = _store.Personnel.Where(p => p.EndDate.HasValue).AsEnumerable();
        if (from.HasValue) list = list.Where(p => p.EndDate >= from);
        if (to.HasValue) list = list.Where(p => p.EndDate <= to);
        return list.Select(p => (object)new
        {
            SicilNo = p.SicilNo,
            FullName = p.FirstName + " " + p.LastName,
            EndDate = p.EndDate,
            Department = _store.Departments.FirstOrDefault(d => d.Id == p.DepartmentId)?.Name
        }).ToList();
    }

    public IReadOnlyList<object> GetExceptionReport()
    {
        return _store.PersonnelAccesses
            .Where(a => a.IsActive && a.IsException)
            .Join(_store.Personnel, a => a.PersonnelId, p => p.Id, (a, p) => new { a, p })
            .Join(_store.ResourceSystems, x => x.a.ResourceSystemId, s => s.Id, (x, s) => (object)new
            {
                Person = x.p.FirstName + " " + x.p.LastName,
                SicilNo = x.p.SicilNo,
                System = s.Name,
                Permission = x.a.PermissionType.ToString(),
                ExpiresAt = x.a.ExpiresAt
            })
            .ToList();
    }
}
