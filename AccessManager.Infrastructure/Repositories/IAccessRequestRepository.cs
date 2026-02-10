using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.Infrastructure.Repositories;

public interface IAccessRequestRepository
{
    IReadOnlyList<AccessRequest> GetAll();
    IReadOnlyList<AccessRequest> GetByPersonnelId(int personnelId);
    IReadOnlyList<AccessRequest> GetByIds(IEnumerable<int> requestIds);
    AccessRequest? GetById(int id);
    int Insert(AccessRequest request);
    void UpdateStatus(int id, AccessRequestStatus status);
}
