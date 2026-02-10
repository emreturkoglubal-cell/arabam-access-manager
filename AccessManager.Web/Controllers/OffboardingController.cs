using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class OffboardingController : Controller
{
    private readonly IPersonnelService _personnelService;
    private readonly IAuditService _auditService;

    public OffboardingController(IPersonnelService personnelService, IAuditService auditService)
    {
        _personnelService = personnelService;
        _auditService = auditService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.ActivePersonnel = _personnelService.GetActive();
        return View(new OffboardingInputModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(OffboardingInputModel input)
    {
        if (!input.SelectedPersonnelId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Personel seçiniz.");
            ViewBag.ActivePersonnel = _personnelService.GetActive();
            return View(input);
        }
        var p = _personnelService.GetById(input.SelectedPersonnelId.Value);
        if (p == null) return NotFound();
        _personnelService.SetOffboarded(input.SelectedPersonnelId.Value, input.EndDate);
        _auditService.Log(AuditAction.PersonnelOffboarded, null, "Sistem", "Personnel", p.Id.ToString(), $"İşten çıkış: {p.FirstName} {p.LastName} - {input.EndDate:dd.MM.yyyy}");
        return RedirectToAction(nameof(Result), new { id = p.Id });
    }

    [HttpGet]
    public IActionResult Result(Guid id)
    {
        var personnel = _personnelService.GetById(id);
        if (personnel == null) return NotFound();
        ViewBag.Personnel = personnel;
        return View();
    }
}

public class OffboardingInputModel
{
    public Guid? SelectedPersonnelId { get; set; }
    public DateTime EndDate { get; set; } = DateTime.Today;
}
