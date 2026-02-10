using AccessManager.Domain.Entities;

namespace AccessManager.Application.Interfaces;

public interface IAccessRequestService
{
    IReadOnlyList<AccessRequest> GetAll();
    IReadOnlyList<AccessRequest> GetPendingForApprover(int approverId);
    IReadOnlyList<AccessRequest> GetByPersonnelId(int personnelId);
    AccessRequest? GetById(int id);
    IReadOnlyList<ApprovalStep> GetApprovalSteps(int requestId);
    AccessRequest Create(AccessRequest request);
    void ApproveStep(int requestId, string stepName, int approverId, string? approverDisplayName, bool approved, string? comment = null);
    /// <param name="appliedById">Talep uygulamasını tetikleyen kullanıcı (giriş yapan). Denetim kaydında "yapan kişi" olarak yazılır.</param>
    /// <param name="appliedByName">Görünen ad (örn. Sistem Yöneticisi).</param>
    void MarkAsApplied(int requestId, int? appliedById = null, string? appliedByName = null);
}
