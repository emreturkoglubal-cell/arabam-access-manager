using AccessManager.Domain.Entities;

namespace AccessManager.Infrastructure.Repositories;

public interface IApprovalStepRepository
{
    IReadOnlyList<ApprovalStep> GetByAccessRequestId(int accessRequestId);
    void Insert(ApprovalStep step);
    ApprovalStep? GetStep(int accessRequestId, string stepName);
    void UpdateApproval(int id, int? approvedBy, string? approvedByName, DateTime? approvedAt, bool? approved, string? comment);
}
