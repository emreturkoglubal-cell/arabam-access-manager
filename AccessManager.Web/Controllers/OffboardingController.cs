using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using AccessManager.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

/// <summary>
/// İşten çıkış (offboarding): personel seçilir, bitiş tarihi verilir; personel durumu Offboarded yapılır. Result sayfasında özet gösterilir.
/// Yetki: Admin veya Manager.
/// </summary>
[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class OffboardingController : Controller
{
    private readonly IPersonnelService _personnelService;
    private readonly IAuditService _auditService;
    private readonly IReportService _reportService;
    private readonly IAssetService _assetService;
    private readonly IPersonnelAccessService _personnelAccessService;
    private readonly ZimmetPdfService _zimmetPdfService;
    private readonly IDepartmentService _departmentService;

    public OffboardingController(
        IPersonnelService personnelService,
        IAuditService auditService,
        IReportService reportService,
        IAssetService assetService,
        IPersonnelAccessService personnelAccessService,
        ZimmetPdfService zimmetPdfService,
        IDepartmentService departmentService)
    {
        _personnelService = personnelService;
        _auditService = auditService;
        _reportService = reportService;
        _assetService = assetService;
        _personnelAccessService = personnelAccessService;
        _zimmetPdfService = zimmetPdfService;
        _departmentService = departmentService;
    }

    /// <summary>GET /Offboarding/Index — İşten çıkış formu; alt listede tarih ve departman filtresi.</summary>
    [HttpGet]
    public IActionResult Index(DateTime? from, DateTime? to, int? departmentId)
    {
        ViewBag.ActivePersonnel = _personnelService.GetActive();
        ViewBag.Departments = _departmentService.GetAll();

        var toDate = (to ?? DateTime.Today).Date;
        var fromDate = (from ?? toDate.AddMonths(-1)).Date;
        if (fromDate > toDate)
            (fromDate, toDate) = (toDate, fromDate);

        ViewBag.FilterFrom = fromDate;
        ViewBag.FilterTo = toDate;
        ViewBag.FilterDepartmentId = departmentId;
        ViewBag.RecentOffboarded = _reportService.GetOffboardedReport(fromDate, toDate, departmentId);
        return View(new OffboardingInputModel());
    }

    /// <summary>POST /Offboarding/Index — Seçilen personeli offboard eder (EndDate ile); Result sayfasına yönlendirir.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(OffboardingInputModel input)
    {
        if (!input.SelectedPersonnelId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Personel seçiniz.");
            ViewBag.ActivePersonnel = _personnelService.GetActive();
            ViewBag.Departments = _departmentService.GetAll();
            var fd = DateTime.Today.AddMonths(-1);
            ViewBag.FilterFrom = fd;
            ViewBag.FilterTo = DateTime.Today;
            ViewBag.FilterDepartmentId = (int?)null;
            ViewBag.RecentOffboarded = _reportService.GetOffboardedReport(fd, DateTime.Today, null);
            return View(input);
        }
        var p = _personnelService.GetById(input.SelectedPersonnelId.Value);
        if (p == null) return NotFound();
        _personnelService.SetOffboarded(input.SelectedPersonnelId.Value, input.EndDate);
        _auditService.Log(AuditAction.PersonnelOffboarded, null, "Sistem", "Personnel", p.Id.ToString(), $"İşten çıkış: {p.FirstName} {p.LastName} - {input.EndDate:dd.MM.yyyy}");
        return RedirectToAction(nameof(Result), new { id = p.Id });
    }

    /// <summary>GET /Offboarding/Result/{id} — İşten çıkış tamamlandı özet sayfası.</summary>
    [HttpGet]
    public IActionResult Result(int id)
    {
        var personnel = _personnelService.GetById(id);
        if (personnel == null) return NotFound();
        ViewBag.Personnel = personnel;
        return View();
    }

    /// <summary>GET /Offboarding/ExitSummaryPdf/{id} — İşten çıkan personel için özet PDF (yetki uyarısı + zimmet tablosu).</summary>
    [HttpGet]
    public IActionResult ExitSummaryPdf(int id)
    {
        var personnel = _personnelService.GetById(id);
        if (personnel == null) return NotFound();

        var accesses = _personnelAccessService.GetByPersonnel(id);
        var hasOpenAccess = accesses.Any(a => a.IsActive);
        var assignments = _assetService.GetAssignmentsByPersonnel(id);
        var rows = new List<(Asset Asset, AssetAssignment Assignment)>();
        foreach (var asn in assignments)
        {
            var asset = _assetService.GetById(asn.AssetId);
            if (asset != null)
                rows.Add((asset, asn));
        }

        var pdfBytes = _zimmetPdfService.GenerateOffboardingExitSummaryPdf(personnel, hasOpenAccess, rows);
        var safeName = $"{personnel.LastName}_{personnel.FirstName}".Replace(' ', '_');
        return File(pdfBytes, "application/pdf", $"IstenCikis_{safeName}_{id}.pdf");
    }
}

/// <summary>İşten çıkış formu: seçilen personel ve bitiş tarihi.</summary>
public class OffboardingInputModel
{
    public int? SelectedPersonnelId { get; set; }
    public DateTime EndDate { get; set; } = DateTime.Today;
}
