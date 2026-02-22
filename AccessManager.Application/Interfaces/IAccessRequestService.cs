using AccessManager.Domain.Entities;

namespace AccessManager.Application.Interfaces;

/// <summary>
/// Erişim talebi (AccessRequest) yönetimi: oluşturma, onay adımları (Yönetici / Sistem Sahibi / IT), talep uygulama (MarkAsApplied) ile PersonnelAccess oluşturulması.
/// </summary>
public interface IAccessRequestService
{
    /// <summary>Tüm erişim taleplerini döner.</summary>
    IReadOnlyList<AccessRequest> GetAll();
    /// <summary>Belirtilen onaylayıcıya (yönetici, sistem sahibi veya IT) bekleyen talepler.</summary>
    IReadOnlyList<AccessRequest> GetPendingForApprover(int approverId);
    /// <summary>Belirtilen personelin talepleri.</summary>
    IReadOnlyList<AccessRequest> GetByPersonnelId(int personnelId);
    /// <summary>ID ile tek talep; yoksa null.</summary>
    AccessRequest? GetById(int id);
    /// <summary>Talebin onay adımlarını (ApprovalStep) kronolojik döner.</summary>
    IReadOnlyList<ApprovalStep> GetApprovalSteps(int requestId);
    /// <summary>Yeni erişim talebi oluşturur; genelde Draft veya ilk onay adımına gönderilir.</summary>
    AccessRequest Create(AccessRequest request);
    /// <summary>Belirtilen onay adımını onaylar veya reddeder; bir sonraki adım veya talep durumu güncellenir.</summary>
    void ApproveStep(int requestId, string stepName, int approverId, string? approverDisplayName, bool approved, string? comment = null);
    /// <param name="appliedById">Talep uygulamasını tetikleyen kullanıcı (giriş yapan). Denetim kaydında "yapan kişi" olarak yazılır.</param>
    /// <param name="appliedByName">Görünen ad (örn. Sistem Yöneticisi).</param>
    void MarkAsApplied(int requestId, int? appliedById = null, string? appliedByName = null);
}
