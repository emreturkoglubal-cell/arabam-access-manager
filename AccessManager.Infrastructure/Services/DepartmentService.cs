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
}
