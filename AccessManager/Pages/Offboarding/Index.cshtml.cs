using AccessManager.Models;
using AccessManager.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.Pages.Offboarding;

public class IndexModel : PageModel
{
    private readonly IPersonnelService _personnelService;
    private readonly IPersonnelAccessService _accessService;
    private readonly IAuditService _auditService;
    private readonly ISystemService _systemService;

    public IndexModel(IPersonnelService personnelService, IPersonnelAccessService accessService, IAuditService auditService, ISystemService systemService)
    {
        _personnelService = personnelService;
        _accessService = accessService;
        _auditService = auditService;
        _systemService = systemService;
    }

    public IReadOnlyList<Models.Personnel> ActivePersonnel { get; set; } = new List<Models.Personnel>();

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
