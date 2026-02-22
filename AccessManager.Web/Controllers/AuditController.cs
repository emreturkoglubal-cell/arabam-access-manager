using AccessManager.Application.Interfaces;
using AccessManager.UI.Constants;
using AccessManager.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

/// <summary>
/// Denetim (audit) log sayfası. Tüm sistem işlemleri (giriş, personel/erişim/rol/sistem/donanım işlemleri) AuditAction türüyle kaydedilir; targetType ile filtrelenebilir, sayfalı.
/// Yetki: Admin veya Auditor.
/// </summary>
[Authorize(Roles = AuthorizationRolePolicies.AdminAndAuditor)]
public class AuditController : Controller
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>GET /Audit/Index — Denetim kayıtlarını listeler; targetType (örn. Personnel, AccessRequest) ve sayfalama ile.</summary>
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
