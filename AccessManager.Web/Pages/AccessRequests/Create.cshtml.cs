using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PersonnelEntity = AccessManager.Domain.Entities.Personnel;

namespace AccessManager.UI.Pages.AccessRequests;

[Authorize(Roles = AuthorizationRolePolicies.AdminManagerUser)]
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

    public IReadOnlyList<PersonnelEntity> PersonnelList { get; set; } = new List<PersonnelEntity>();
    public IReadOnlyList<ResourceSystem> Systems { get; set; } = new List<ResourceSystem>();
    /// <summary>Personel detayından gelindiyse dolu; bu durumda personel alanı dropdown yerine sadece isim gösterilir.</summary>
    public PersonnelEntity? PreselectedPerson { get; set; }

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
        if (personnelId.HasValue && personnelId.Value != Guid.Empty)
        {
            var p = _personnelService.GetById(personnelId.Value);
            if (p != null)
            {
                Input.PersonnelId = personnelId.Value;
                PreselectedPerson = p;
            }
        }
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
