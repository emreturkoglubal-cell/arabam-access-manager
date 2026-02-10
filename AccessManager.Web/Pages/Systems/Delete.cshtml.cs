using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.UI.Pages.Systems;

[Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
public class DeleteModel : PageModel
{
    private readonly ISystemService _systemService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;

    public DeleteModel(ISystemService systemService, IAuditService auditService, ICurrentUserService currentUser)
    {
        _systemService = systemService;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    public ResourceSystem? System { get; set; }
    public bool DeleteFailed { get; set; }
    public string? DeleteFailedMessage { get; set; }

    public IActionResult OnGet(int id)
    {
        System = _systemService.GetById(id);
        if (System == null) return NotFound();
        return Page();
    }

    public IActionResult OnPost(int id)
    {
        System = _systemService.GetById(id);
        if (System == null) return NotFound();

        var deleted = _systemService.Delete(id);
        if (!deleted)
        {
            DeleteFailed = true;
            DeleteFailedMessage = "Bu sistem yetki talebi, rol yetkisi veya personel erişiminde kullanıldığı için silinemiyor. Önce ilgili kayıtları güncelleyin.";
            return Page();
        }

        var actorId = _currentUser.UserId;
        var actorName = _currentUser.DisplayName ?? _currentUser.UserName ?? "?";
        _auditService.Log(AuditAction.SystemDeleted, actorId, actorName, "ResourceSystem", id.ToString(), $"Silinen sistem: {System.Name}");

        return RedirectToPage("Index");
    }
}
