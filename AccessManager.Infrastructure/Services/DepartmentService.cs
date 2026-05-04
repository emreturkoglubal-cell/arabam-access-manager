using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Infrastructure.Repositories;

namespace AccessManager.Infrastructure.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _repo;
    private readonly IDepartmentManagerRepository _managerRepo;

    public DepartmentService(IDepartmentRepository repo, IDepartmentManagerRepository managerRepo)
    {
        _repo = repo;
        _managerRepo = managerRepo;
    }

    public IReadOnlyList<Department> GetAll() => _repo.GetAll();

    public Department? GetById(int id) => _repo.GetById(id);

    public Department Add(string name, string? code, string? description, int? parentId = null, int? topManagerPersonnelId = null)
    {
        var department = new Department
        {
            Name = name?.Trim() ?? string.Empty,
            Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ParentId = parentId,
            TopManagerPersonnelId = topManagerPersonnelId
        };
        department.Id = _repo.Insert(department);
        return department;
    }

    public void Update(Department department)
    {
        ArgumentNullException.ThrowIfNull(department);
        _repo.Update(department);
    }

    public IReadOnlyList<DepartmentManager> GetDepartmentManagers(int departmentId) => _managerRepo.GetByDepartmentId(departmentId);

    public void SetDepartmentManagers(int departmentId, IReadOnlyList<(int PersonnelId, short Level)> managers) => _managerRepo.SetForDepartment(departmentId, managers ?? Array.Empty<(int, short)>());
}
