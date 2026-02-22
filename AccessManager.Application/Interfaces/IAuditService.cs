using AccessManager.Application.Dtos;
using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.Application.Interfaces;

/// <summary>
/// Denetim günlüğü (AuditLog): tüm önemli işlemlere Log ile kayıt, targetType/targetId ve tarih aralığı ile sorgulama.
/// </summary>
public interface IAuditService
{
    /// <summary>Bir işlem için denetim kaydı yazar (AuditAction, hedef tip/id, detay, isteğe bağlı IP).</summary>
    void Log(AuditAction action, int? actorId, string actorName, string targetType, string? targetId, string? details = null, string? ipAddress = null);
    /// <summary>En son N adet denetim kaydı (varsayılan 100).</summary>
    IReadOnlyList<AuditLog> GetRecent(int count = 100);
    /// <summary>Hedef türü (ve isteğe bağlı hedef ID) ile filtreler.</summary>
    IReadOnlyList<AuditLog> GetByTarget(string targetType, string? targetId = null);
    /// <summary>Tarih aralığına göre kayıtlar.</summary>
    IReadOnlyList<AuditLog> GetByDateRange(DateTime from, DateTime to);
    /// <summary>Hedef türü ile sayfalı denetim listesi (Audit sayfası için).</summary>
    PagedResult<AuditLog> GetPaged(string? targetType, int page, int pageSize);
}
