using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.UI.Pages.Audit;

[Authorize(Roles = AuthorizationRolePolicies.AdminAndAuditor)]
public class IndexModel : PageModel
{
    private readonly IAuditService _auditService;

    public IndexModel(IAuditService auditService)
    {
        _auditService = auditService;
    }

    public IReadOnlyList<AuditLog> Logs { get; set; } = new List<AuditLog>();
    public string? FilterTargetType { get; set; }

    public void OnGet(string? targetType)
    {
        FilterTargetType = targetType;
        if (!string.IsNullOrEmpty(targetType))
            Logs = _auditService.GetByTarget(targetType);
        else
            Logs = _auditService.GetRecent(200);
    }
}
