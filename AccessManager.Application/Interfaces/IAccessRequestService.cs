using AccessManager.Domain.Entities;

namespace AccessManager.Application.Interfaces;

public interface IAccessRequestService
{
    IReadOnlyList<AccessRequest> GetAll();
    IReadOnlyList<AccessRequest> GetPendingForApprover(Guid approverId);
    IReadOnlyList<AccessRequest> GetByPersonnelId(Guid personnelId);
    AccessRequest? GetById(Guid id);
    IReadOnlyList<ApprovalStep> GetApprovalSteps(Guid requestId);
    AccessRequest Create(AccessRequest request);
    void ApproveStep(Guid requestId, string stepName, Guid approverId, string? approverDisplayName, bool approved, string? comment = null);
    /// <param name="appliedById">Talep uygulamasını tetikleyen kullanıcı (giriş yapan). Denetim kaydında "yapan kişi" olarak yazılır.</param>
    /// <param name="appliedByName">Görünen ad (örn. Sistem Yöneticisi).</param>
    void MarkAsApplied(Guid requestId, Guid? appliedById = null, string? appliedByName = null);
}
