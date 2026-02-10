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
public class CreateModel : PageModel
{
    private readonly ISystemService _systemService;
    private readonly IPersonnelService _personnelService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;

    public CreateModel(ISystemService systemService, IPersonnelService personnelService, IAuditService auditService, ICurrentUserService currentUser)
    {
        _systemService = systemService;
        _personnelService = personnelService;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    public IReadOnlyList<PersonnelEntity> PersonnelList { get; set; } = new List<PersonnelEntity>();

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IActionResult OnGet()
    {
        PersonnelList = _personnelService.GetActive();
        return Page();
    }

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(Input.Name))
        {
            ModelState.AddModelError(nameof(Input.Name), "Sistem adı gerekli.");
            PersonnelList = _personnelService.GetActive();
            return Page();
        }

        var system = new ResourceSystem
        {
            Name = Input.Name.Trim(),
            Code = string.IsNullOrWhiteSpace(Input.Code) ? null : Input.Code.Trim(),
            SystemType = Input.SystemType,
            CriticalLevel = Input.CriticalLevel,
            OwnerId = Input.OwnerId == 0 ? null : Input.OwnerId,
            Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim()
        };
        _systemService.Create(system);

        var actorId = _currentUser.UserId;
        var actorName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
        _auditService.Log(AuditAction.SystemCreated, actorId, actorName, "ResourceSystem", system.Id.ToString(), $"Sistem: {system.Name}");

        return RedirectToPage("Index");
    }

    public class InputModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public SystemType SystemType { get; set; } = SystemType.Application;
        public CriticalLevel CriticalLevel { get; set; } = CriticalLevel.Medium;
        public int OwnerId { get; set; }
        public string? Description { get; set; }
    }
}
