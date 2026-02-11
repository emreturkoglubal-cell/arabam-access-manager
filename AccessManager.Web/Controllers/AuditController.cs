using AccessManager.Application.Interfaces;
using AccessManager.UI.Constants;
using AccessManager.UI.ViewModels;
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
    public IActionResult Index(string? targetType, int page = 1, int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var paged = _auditService.GetPaged(targetType, page, pageSize);
        var model = new AuditIndexViewModel
        {
            Logs = paged.Items,
            FilterTargetType = targetType,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount
        };
        return View(model);
    }
}
