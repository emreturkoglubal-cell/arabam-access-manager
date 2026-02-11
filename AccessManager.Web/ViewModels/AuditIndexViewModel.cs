using AccessManager.Domain.Entities;

namespace AccessManager.UI.ViewModels;

public class AuditIndexViewModel
{
    public IReadOnlyList<AuditLog> Logs { get; set; } = new List<AuditLog>();
    public string? FilterTargetType { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
