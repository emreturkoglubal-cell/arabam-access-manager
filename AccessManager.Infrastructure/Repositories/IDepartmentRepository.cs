using AccessManager.Domain.Entities;

namespace AccessManager.Infrastructure.Repositories;

public interface IDepartmentRepository
{
    IReadOnlyList<Department> GetAll();
    Department? GetById(int id);
}
