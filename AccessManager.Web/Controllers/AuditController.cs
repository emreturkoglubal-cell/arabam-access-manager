using AccessManager.Application.Interfaces;
using AccessManager.UI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

[Authorize(Roles = AuthorizationRolePolicies.AdminAndAuditor)]
public class AuditController : Controller
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public IActionResult Index(string? targetType)
    {
        var logs = !string.IsNullOrEmpty(targetType)
            ? _auditService.GetByTarget(targetType)
            : _auditService.GetRecent(200);
        ViewBag.Logs = logs;
        ViewBag.FilterTargetType = targetType;
        return View();
    }
}
