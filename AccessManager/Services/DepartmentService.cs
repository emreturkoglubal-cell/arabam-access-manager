using AccessManager.Data;
using AccessManager.Models;

namespace AccessManager.Services;

public class DepartmentService : IDepartmentService
{
    private readonly MockDataStore _store = MockDataStore.Current;

    public IReadOnlyList<Department> GetAll() => _store.Departments.ToList();

    public Department? GetById(Guid id) => _store.Departments.FirstOrDefault(d => d.Id == id);
}
