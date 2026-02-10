using AccessManager.Models;

namespace AccessManager.Services;

public interface IDepartmentService
{
    IReadOnlyList<Department> GetAll();
    Department? GetById(Guid id);
}
