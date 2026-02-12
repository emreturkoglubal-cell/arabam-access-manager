using AccessManager.Application;
using AccessManager.Application.Dtos;
using AccessManager.Application.Interfaces;
using AccessManager.Domain.Constants;
using AccessManager.Domain.Enums;
using AccessManager.Infrastructure.Repositories;

namespace AccessManager.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly IPersonnelRepository _personnelRepo;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly IAccessRequestRepository _requestRepo;
    private readonly IPersonnelAccessRepository _accessRepo;
    private readonly IResourceSystemRepository _systemRepo;

    public ReportService(
        IPersonnelRepository personnelRepo,
        IDepartmentRepository departmentRepo,
        IAccessRequestRepository requestRepo,
        IPersonnelAccessRepository accessRepo,
        IResourceSystemRepository systemRepo)
    {
        _personnelRepo = personnelRepo;
        _departmentRepo = departmentRepo;
        _requestRepo = requestRepo;
        _accessRepo = accessRepo;
        _systemRepo = systemRepo;
    }

    public DashboardStats GetDashboardStats(int? departmentId = null, int? periodMonths = null)
    {
        var now = SystemTime.Now;
        var months = periodMonths.HasValue && periodMonths.Value > 0 ? Math.Min(periodMonths.Value, 120) : 1;
        var periodStart = now.AddMonths(-months).Date;

        var personnelInScope = departmentId.HasValue
            ? _personnelRepo.GetByDepartmentId(departmentId.Value).Select(p => p.Id).ToHashSet()
            : null;

        bool InScope(int pid) => personnelInScope == null || personnelInScope.Contains(pid);

        var activePersonnel = _personnelRepo.GetActive().Where(p => personnelInScope == null || personnelInScope.Contains(p.Id)).ToList();
        var allPersonnel = _personnelRepo.GetAll();
        var offboardedInPeriod = allPersonnel.Count(p => p.EndDate.HasValue && p.EndDate >= periodStart && InScope(p.Id));

        var pendingRequests = _requestRepo.GetAll().Where(r =>
            r.Status == AccessRequestStatus.PendingManager || r.Status == AccessRequestStatus.PendingSystemOwner || r.Status == AccessRequestStatus.PendingIT);
        var pendingCount = personnelInScope == null
            ? pendingRequests.Count()
            : pendingRequests.Count(r => personnelInScope.Contains(r.PersonnelId));

        var expiring = _accessRepo.GetExpiringWithinDays(30).Count(a => InScope(a.PersonnelId));
        var exceptions = _accessRepo.GetExceptions().Count(a => InScope(a.PersonnelId));

        return new DashboardStats
        {
            ActivePersonnelCount = activePersonnel.Count,
            OffboardedLastMonthCount = offboardedInPeriod,
            PendingRequestsCount = pendingCount,
            ExpiringAccessCount = expiring,
            ExceptionAccessCount = exceptions
        };
    }

    public IReadOnlyList<AccessBySystemReportRow> GetAccessReportBySystem()
    {
        var systems = _systemRepo.GetAll();
        if (systems.Count == 0) return new List<AccessBySystemReportRow>();

        var activeAccesses = _accessRepo.GetActive();
        return systems.Select(s => new AccessBySystemReportRow
        {
            SystemName = s.Name ?? string.Empty,
            SystemCode = s.Code ?? string.Empty,
            ActiveAccessCount = activeAccesses.Count(a => a.ResourceSystemId == s.Id)
        }).ToList();
    }

    public IReadOnlyList<OffboardedReportRow> GetOffboardedReport(DateTime? from, DateTime? to)
    {
        var list = _personnelRepo.GetAll().Where(p => p.EndDate.HasValue).AsEnumerable();
        if (from.HasValue) list = list.Where(p => p.EndDate >= from);
        if (to.HasValue) list = list.Where(p => p.EndDate <= to);
        var departments = _departmentRepo.GetAll().ToDictionary(d => d.Id);
        return list.Select(p => new OffboardedReportRow
        {
            PersonnelId = p.Id,
            FullName = $"{p.FirstName} {p.LastName}".Trim(),
            EndDate = p.EndDate,
            Department = departments.GetValueOrDefault(p.DepartmentId)?.Name
        }).ToList();
    }

    public IReadOnlyList<ExceptionReportRow> GetExceptionReport()
    {
        var exceptions = _accessRepo.GetExceptions();
        var personnelIds = exceptions.Select(a => a.PersonnelId).Distinct().ToList();
        var systemIds = exceptions.Select(a => a.ResourceSystemId).Distinct().ToList();
        var personnel = _personnelRepo.GetAll().Where(p => personnelIds.Contains(p.Id)).ToDictionary(p => p.Id);
        var systems = _systemRepo.GetAll().Where(s => systemIds.Contains(s.Id)).ToDictionary(s => s.Id);

        return exceptions.Select(a =>
        {
            var p = personnel.GetValueOrDefault(a.PersonnelId);
            var s = systems.GetValueOrDefault(a.ResourceSystemId);
            return new ExceptionReportRow
            {
                PersonnelId = a.PersonnelId,
                Person = p != null ? $"{p.FirstName} {p.LastName}".Trim() : "?",
                System = s?.Name ?? string.Empty,
                Permission = PermissionTypeLabels.Get(a.PermissionType),
                ExpiresAt = a.ExpiresAt
            };
        }).ToList();
    }
}
