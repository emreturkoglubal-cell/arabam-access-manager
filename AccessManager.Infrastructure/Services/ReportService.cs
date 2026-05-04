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
    private readonly ICurrencyService _currencyService;

    public ReportService(
        IPersonnelRepository personnelRepo,
        IDepartmentRepository departmentRepo,
        IAccessRequestRepository requestRepo,
        IPersonnelAccessRepository accessRepo,
        IResourceSystemRepository systemRepo,
        ICurrencyService currencyService)
    {
        _personnelRepo = personnelRepo;
        _departmentRepo = departmentRepo;
        _requestRepo = requestRepo;
        _accessRepo = accessRepo;
        _systemRepo = systemRepo;
        _currencyService = currencyService;
    }

    public DashboardStats GetDashboardStats(int? departmentId = null, int? periodMonths = null, DateTime? periodFrom = null, DateTime? periodTo = null)
    {
        var now = SystemTime.Now;

        var personnelInScope = departmentId.HasValue
            ? _personnelRepo.GetByDepartmentId(departmentId.Value).Select(p => p.Id).ToHashSet()
            : null;

        bool InScope(int pid) => personnelInScope == null || personnelInScope.Contains(pid);

        var activePersonnel = _personnelRepo.GetActive().Where(p => personnelInScope == null || personnelInScope.Contains(p.Id)).ToList();
        var allPersonnel = _personnelRepo.GetAll();

        int offboardedInPeriod;
        if (periodFrom.HasValue && periodTo.HasValue)
        {
            var from = periodFrom.Value.Date;
            var to = periodTo.Value.Date;
            if (from > to)
                (from, to) = (to, from);
            var maxEnd = from.AddYears(10);
            if (to > maxEnd)
                to = maxEnd;

            offboardedInPeriod = allPersonnel.Count(p =>
                p.EndDate.HasValue &&
                p.EndDate.Value.Date >= from &&
                p.EndDate.Value.Date <= to &&
                InScope(p.Id));
        }
        else
        {
            var months = periodMonths.HasValue && periodMonths.Value > 0 ? Math.Min(periodMonths.Value, 120) : 1;
            var periodStart = now.AddMonths(-months).Date;
            offboardedInPeriod = allPersonnel.Count(p => p.EndDate.HasValue && p.EndDate >= periodStart && InScope(p.Id));
        }

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

    public DashboardChartData GetDashboardChartData(int? departmentId = null, int periodMonths = 12, DateTime? rangeFrom = null, DateTime? rangeTo = null)
    {
        var now = SystemTime.Now;
        var personnelInScope = departmentId.HasValue
            ? _personnelRepo.GetByDepartmentId(departmentId.Value).Select(p => p.Id).ToHashSet()
            : null;
        bool InScope(int pid) => personnelInScope == null || personnelInScope.Contains(pid);

        var allPersonnel = _personnelRepo.GetAll().Where(p => InScope(p.Id)).ToList();
        var personnelTrend = new List<MonthCountPair>();
        var offboardedByMonth = new List<MonthCountPair>();

        var culture = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
        const int chartMonthCap = 48;

        IEnumerable<DateTime> MonthStarts()
        {
            if (rangeFrom.HasValue && rangeTo.HasValue)
            {
                var from = rangeFrom.Value.Date;
                var to = rangeTo.Value.Date;
                if (from > to)
                    (from, to) = (to, from);
                var startMonth = new DateTime(from.Year, from.Month, 1, 0, 0, 0, DateTimeKind.Local);
                var endMonth = new DateTime(to.Year, to.Month, 1, 0, 0, 0, DateTimeKind.Local);
                var n = 0;
                for (var m = startMonth; m <= endMonth && n < chartMonthCap; m = m.AddMonths(1), n++)
                    yield return m;
            }
            else
            {
                var months = Math.Clamp(periodMonths, 1, 24);
                for (var i = months - 1; i >= 0; i--)
                {
                    var anchor = now.AddMonths(-i);
                    yield return new DateTime(anchor.Year, anchor.Month, 1, 0, 0, 0, DateTimeKind.Local);
                }
            }
        }

        foreach (var monthStart in MonthStarts())
        {
            var monthEndDate = monthStart.AddMonths(1).AddDays(-1);

            var activeAtMonthEnd = allPersonnel.Count(p =>
                p.StartDate <= monthEndDate &&
                (!p.EndDate.HasValue || p.EndDate.Value > monthEndDate));

            personnelTrend.Add(new MonthCountPair
            {
                Label = monthStart.ToString("MMM yyyy", culture),
                Count = activeAtMonthEnd
            });

            var offboardedInThisMonth = allPersonnel.Count(p =>
                p.EndDate.HasValue &&
                p.EndDate.Value.Year == monthStart.Year &&
                p.EndDate.Value.Month == monthStart.Month);
            offboardedByMonth.Add(new MonthCountPair
            {
                Label = monthStart.ToString("MMM yyyy", culture),
                Count = offboardedInThisMonth
            });
        }

        var accessBySystem = GetAccessReportBySystem()
            .OrderByDescending(r => r.ActiveAccessCount)
            .Take(10)
            .Select(r => new LabelCountPair { Label = string.IsNullOrEmpty(r.SystemName) ? r.SystemCode : r.SystemName, Count = r.ActiveAccessCount })
            .ToList();

        var deptCounts = _personnelRepo.GetPersonnelCountByDepartment();
        var departments = _departmentRepo.GetAll().ToDictionary(d => d.Id, d => d.Name ?? "—");
        var personnelByDepartment = (departmentId.HasValue
            ? new Dictionary<int, int> { { departmentId.Value, deptCounts.GetValueOrDefault(departmentId.Value) } }
            : deptCounts)
            .Select(kv => new LabelCountPair { Label = departments.GetValueOrDefault(kv.Key) ?? "—", Count = kv.Value })
            .Where(x => x.Count > 0)
            .OrderByDescending(x => x.Count)
            .ToList();

        return new DashboardChartData
        {
            PersonnelTrend = personnelTrend,
            OffboardedByMonth = offboardedByMonth,
            AccessBySystem = accessBySystem,
            PersonnelByDepartment = personnelByDepartment
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

    public IReadOnlyList<OffboardedReportRow> GetOffboardedReport(DateTime? from, DateTime? to, int? departmentId = null)
    {
        var list = _personnelRepo.GetAll().Where(p => p.EndDate.HasValue).AsEnumerable();
        if (departmentId.HasValue) list = list.Where(p => p.DepartmentId == departmentId.Value);
        if (from.HasValue) list = list.Where(p => p.EndDate >= from);
        if (to.HasValue) list = list.Where(p => p.EndDate <= to);
        var departments = _departmentRepo.GetAll().ToDictionary(d => d.Id);
        var activeAccessPersonnelIds = _accessRepo.GetActive().Select(a => a.PersonnelId).ToHashSet();
        return list.Select(p => new OffboardedReportRow
        {
            PersonnelId = p.Id,
            FullName = $"{p.FirstName} {p.LastName}".Trim(),
            Email = p.Email,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Department = departments.GetValueOrDefault(p.DepartmentId)?.Name,
            HasOpenAccess = activeAccessPersonnelIds.Contains(p.Id)
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

    public ReportsIndexData GetReportsIndexData(DateTime? from, DateTime? to)
    {
        // Tek turda tüm ham veriyi çek (tekrarlı sorguları kaldırır)
        var allPersonnel = _personnelRepo.GetAll();
        var departments = _departmentRepo.GetAll().ToDictionary(d => d.Id);
        var systems = _systemRepo.GetAll();
        var activeAccesses = _accessRepo.GetActive();
        var allRequests = _requestRepo.GetAll();
        var expiringAccesses = _accessRepo.GetExpiringWithinDays(30);
        var exceptionAccesses = _accessRepo.GetExceptions();

        var activePersonnel = allPersonnel.Where(p => p.Status == PersonnelStatus.Active).ToList();
        var now = SystemTime.Now;
        var periodStart = now.AddMonths(-1).Date;
        var offboardedInPeriod = allPersonnel.Count(p => p.EndDate.HasValue && p.EndDate >= periodStart);
        var pendingRequests = allRequests.Where(r =>
            r.Status == AccessRequestStatus.PendingManager || r.Status == AccessRequestStatus.PendingSystemOwner || r.Status == AccessRequestStatus.PendingIT);

        var stats = new DashboardStats
        {
            ActivePersonnelCount = activePersonnel.Count,
            OffboardedLastMonthCount = offboardedInPeriod,
            PendingRequestsCount = pendingRequests.Count(),
            ExpiringAccessCount = expiringAccesses.Count,
            ExceptionAccessCount = exceptionAccesses.Count
        };

        var accessBySystem = systems.Select(s => new AccessBySystemReportRow
        {
            SystemName = s.Name ?? string.Empty,
            SystemCode = s.Code ?? string.Empty,
            ActiveAccessCount = activeAccesses.Count(a => a.ResourceSystemId == s.Id)
        }).ToList();

        var offboardedList = allPersonnel.Where(p => p.EndDate.HasValue).AsEnumerable();
        if (from.HasValue) offboardedList = offboardedList.Where(p => p.EndDate >= from);
        if (to.HasValue) offboardedList = offboardedList.Where(p => p.EndDate <= to);
        var activeAccessPersonnelIds = activeAccesses.Where(a => a.IsActive).Select(a => a.PersonnelId).ToHashSet();
        var offboardedReport = offboardedList.Select(p => new OffboardedReportRow
        {
            PersonnelId = p.Id,
            FullName = $"{p.FirstName} {p.LastName}".Trim(),
            Email = p.Email,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Department = departments.GetValueOrDefault(p.DepartmentId)?.Name,
            HasOpenAccess = activeAccessPersonnelIds.Contains(p.Id)
        }).ToList();

        var personnelDict = allPersonnel.ToDictionary(p => p.Id);
        var systemsDict = systems.ToDictionary(s => s.Id);
        var exceptionReport = exceptionAccesses.Select(a =>
        {
            var p = personnelDict.GetValueOrDefault(a.PersonnelId);
            var s = systemsDict.GetValueOrDefault(a.ResourceSystemId);
            return new ExceptionReportRow
            {
                PersonnelId = a.PersonnelId,
                Person = p != null ? $"{p.FirstName} {p.LastName}".Trim() : "?",
                System = s?.Name ?? string.Empty,
                Permission = PermissionTypeLabels.Get(a.PermissionType),
                ExpiresAt = a.ExpiresAt
            };
        }).ToList();

        return new ReportsIndexData
        {
            Stats = stats,
            AccessBySystem = accessBySystem,
            OffboardedReport = offboardedReport,
            ExceptionReport = exceptionReport
        };
    }

    public IReadOnlyList<DepartmentTurnoverPoint> GetDepartmentTurnoverPoints(int departmentId, int months = 12)
    {
        months = Math.Clamp(months, 1, 24);
        var now = SystemTime.Now.Date;
        var anchor = new DateTime(now.Year, now.Month, 1);
        var allInDept = _personnelRepo.GetByDepartmentId(departmentId);
        var ci = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
        var list = new List<DepartmentTurnoverPoint>();
        for (var i = months - 1; i >= 0; i--)
        {
            var m = anchor.AddMonths(-i);
            var hires = allInDept.Count(p => p.StartDate.Year == m.Year && p.StartDate.Month == m.Month);
            var exits = allInDept.Count(p => p.EndDate.HasValue && p.EndDate.Value.Year == m.Year && p.EndDate.Value.Month == m.Month);
            list.Add(new DepartmentTurnoverPoint
            {
                Label = m.ToString("MMM yyyy", ci),
                Hires = hires,
                Exits = exits
            });
        }
        return list;
    }

    public decimal? GetDepartmentActiveLicenseCostUsd(int departmentId)
    {
        var activeInDept = _personnelRepo.GetActive().Where(p => p.DepartmentId == departmentId).Select(p => p.Id).ToHashSet();
        if (activeInDept.Count == 0) return null;
        var accesses = _accessRepo.GetActive().Where(a => activeInDept.Contains(a.PersonnelId)).ToList();
        if (accesses.Count == 0) return null;
        var systems = _systemRepo.GetAll().ToDictionary(s => s.Id);
        var rates = _currencyService.GetRatesToUsd();
        decimal totalUsd = 0;
        foreach (var a in accesses)
        {
            if (!systems.TryGetValue(a.ResourceSystemId, out var sys) || !sys.UnitCost.HasValue) continue;
            var currency = string.IsNullOrWhiteSpace(sys.UnitCostCurrency) ? "TRY" : sys.UnitCostCurrency.Trim().ToUpperInvariant();
            if (rates.TryGetValue(currency, out var rate))
                totalUsd += sys.UnitCost!.Value * rate;
        }
        return totalUsd > 0 ? totalUsd : null;
    }
}
