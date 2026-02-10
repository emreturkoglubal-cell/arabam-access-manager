using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PersonnelEntity = AccessManager.Domain.Entities.Personnel;

namespace AccessManager.UI.Pages.Systems;

[Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
public class EditModel : PageModel
{
    private readonly ISystemService _systemService;
    private readonly IPersonnelService _personnelService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;

    public EditModel(ISystemService systemService, IPersonnelService personnelService, IAuditService auditService, ICurrentUserService currentUser)
    {
        _systemService = systemService;
        _personnelService = personnelService;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    public ResourceSystem? System { get; set; }
    public IReadOnlyList<PersonnelEntity> PersonnelList { get; set; } = new List<PersonnelEntity>();

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IActionResult OnGet(int id)
    {
        System = _systemService.GetById(id);
        if (System == null) return NotFound();
        PersonnelList = _personnelService.GetActive();
        Input.Name = System.Name;
        Input.Code = System.Code;
        Input.SystemType = System.SystemType;
        Input.CriticalLevel = System.CriticalLevel;
        Input.OwnerId = System.OwnerId ?? 0;
        Input.Description = System.Description;
        return Page();
    }

    public IActionResult OnPost(int id)
    {
        System = _systemService.GetById(id);
        if (System == null) return NotFound();

        if (string.IsNullOrWhiteSpace(Input.Name))
        {
            ModelState.AddModelError(nameof(Input.Name), "Sistem adı gerekli.");
            PersonnelList = _personnelService.GetActive();
            return Page();
        }

        System.Name = Input.Name.Trim();
        System.Code = string.IsNullOrWhiteSpace(Input.Code) ? null : Input.Code.Trim();
        System.SystemType = Input.SystemType;
        System.CriticalLevel = Input.CriticalLevel;
        System.OwnerId = Input.OwnerId == 0 ? null : Input.OwnerId;
        System.Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim();
        _systemService.Update(System);

        var actorId = _currentUser.UserId;
        var actorName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
        _auditService.Log(AuditAction.SystemUpdated, actorId, actorName, "ResourceSystem", System.Id.ToString(), $"Sistem: {System.Name}");

        return RedirectToPage("Index");
    }

    public class InputModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public SystemType SystemType { get; set; }
        public CriticalLevel CriticalLevel { get; set; }
        public int OwnerId { get; set; }
        public string? Description { get; set; }
    }
}
