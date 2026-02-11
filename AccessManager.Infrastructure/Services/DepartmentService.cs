using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Infrastructure.Repositories;

namespace AccessManager.Infrastructure.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _repo;

    public DepartmentService(IDepartmentRepository repo)
    {
        _repo = repo;
    }

    public IReadOnlyList<Department> GetAll() => _repo.GetAll();

    public Department? GetById(int id) => _repo.GetById(id);

    public Department Add(string name, string? code, string? description)
    {
        var department = new Department
        {
            Name = name?.Trim() ?? string.Empty,
            Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
        };
        department.Id = _repo.Insert(department);
        return department;
    }
}
