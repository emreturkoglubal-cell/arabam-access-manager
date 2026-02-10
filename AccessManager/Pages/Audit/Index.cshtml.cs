using AccessManager.Models;
using AccessManager.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessManager.Pages.Audit;

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
