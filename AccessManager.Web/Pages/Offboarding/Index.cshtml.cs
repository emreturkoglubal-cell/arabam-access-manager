using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PersonnelEntity = AccessManager.Domain.Entities.Personnel;

namespace AccessManager.UI.Pages.Offboarding;

[Authorize(Roles = AuthorizationRolePolicies.AdminAndManager)]
public class IndexModel : PageModel
{
    private readonly IPersonnelService _personnelService;
    private readonly IAuditService _auditService;

    public IndexModel(IPersonnelService personnelService, IAuditService auditService)
    {
        _personnelService = personnelService;
        _auditService = auditService;
    }

    public IReadOnlyList<PersonnelEntity> ActivePersonnel { get; set; } = new List<PersonnelEntity>();

    [BindProperty]
    public Guid? SelectedPersonnelId { get; set; }

    [BindProperty]
    public DateTime EndDate { get; set; } = DateTime.Today;

    public void OnGet()
    {
        ActivePersonnel = _personnelService.GetActive();
    }

    public IActionResult OnPost()
    {
        if (!SelectedPersonnelId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Personel seçiniz.");
            OnGet();
            return Page();
        }
        var p = _personnelService.GetById(SelectedPersonnelId.Value);
        if (p == null) return NotFound();
        _personnelService.SetOffboarded(SelectedPersonnelId.Value, EndDate);
        _auditService.Log(AuditAction.PersonnelOffboarded, null, "Sistem", "Personnel", p.Id.ToString(), $"İşten çıkış: {p.FirstName} {p.LastName} - {EndDate:dd.MM.yyyy}");
        return RedirectToPage("Result", new { id = p.Id });
    }
}
