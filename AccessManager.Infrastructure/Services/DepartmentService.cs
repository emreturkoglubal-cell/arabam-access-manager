using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Infrastructure.Data;

namespace AccessManager.Infrastructure.Services;

public class DepartmentService : IDepartmentService
{
    private readonly MockDataStore _store;

    public DepartmentService(MockDataStore store)
    {
        _store = store;
    }

    public IReadOnlyList<Department> GetAll() => _store.Departments.ToList();

    public Department? GetById(Guid id) => _store.Departments.FirstOrDefault(d => d.Id == id);
}
