using AccessManager.Domain.Entities;

namespace AccessManager.Infrastructure.Repositories;

public interface IDepartmentManagerRepository
{
    IReadOnlyList<DepartmentManager> GetByDepartmentId(int departmentId);
    void SetForDepartment(int departmentId, IReadOnlyList<(int PersonnelId, short Level)> managers);
}
