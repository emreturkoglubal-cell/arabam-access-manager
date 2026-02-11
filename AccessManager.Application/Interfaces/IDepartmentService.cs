using AccessManager.Domain.Entities;

namespace AccessManager.Application.Interfaces;

public interface IDepartmentService
{
    IReadOnlyList<Department> GetAll();
    Department? GetById(int id);
    Department Add(string name, string? code, string? description);
}
