using AccessManager.Models;
using AccessManager.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.Pages.AccessRequests;

public class CreateModel : PageModel
{
    private readonly IAccessRequestService _requestService;
    private readonly IPersonnelService _personnelService;
    private readonly ISystemService _systemService;

    public CreateModel(IAccessRequestService requestService, IPersonnelService personnelService, ISystemService systemService)
    {
        _requestService = requestService;
        _personnelService = personnelService;
        _systemService = systemService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<Models.Personnel> PersonnelList { get; set; } = new List<Models.Personnel>();
    public IReadOnlyList<ResourceSystem> Systems { get; set; } = new List<ResourceSystem>();

    public class InputModel
    {
        public Guid PersonnelId { get; set; }
        public Guid ResourceSystemId { get; set; }
        public PermissionType RequestedPermission { get; set; }
        public string? Reason { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public void OnGet(Guid? personnelId)
    {
        PersonnelList = _personnelService.GetActive();
        Systems = _systemService.GetAll();
        if (personnelId.HasValue) Input.PersonnelId = personnelId.Value;
    }

    public IActionResult OnPost()
    {
        if (Input.PersonnelId == Guid.Empty || Input.ResourceSystemId == Guid.Empty)
        {
            PersonnelList = _personnelService.GetActive();
            Systems = _systemService.GetAll();
            ModelState.AddModelError(string.Empty, "Personel ve sistem seçiniz.");
            return Page();
        }
        var person = _personnelService.GetById(Input.PersonnelId);
        if (person == null) return NotFound();
        var request = new AccessRequest
        {
            PersonnelId = Input.PersonnelId,
            ResourceSystemId = Input.ResourceSystemId,
            RequestedPermission = Input.RequestedPermission,
            Reason = Input.Reason,
            EndDate = Input.EndDate,
            CreatedBy = Input.PersonnelId
        };
        _requestService.Create(request);
        return RedirectToPage("Detail", new { id = request.Id });
    }
}
