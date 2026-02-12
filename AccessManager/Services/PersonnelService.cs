using AccessManager.Data;
using AccessManager.Models;

namespace AccessManager.Services;

public class PersonnelService : IPersonnelService
{
    private readonly MockDataStore _store = MockDataStore.Current;

    public IReadOnlyList<Personnel> GetAll() => _store.Personnel.ToList();

    public IReadOnlyList<Personnel> GetActive() =>
        _store.Personnel.Where(p => p.Status == PersonnelStatus.Active).ToList();

    public Personnel? GetById(Guid id) => _store.Personnel.FirstOrDefault(p => p.Id == id);

    public IReadOnlyList<Personnel> GetByManagerId(Guid managerId) =>
        _store.Personnel.Where(p => p.ManagerId == managerId).ToList();

    public IReadOnlyList<Personnel> GetByDepartmentId(Guid departmentId) =>
        _store.Personnel.Where(p => p.DepartmentId == departmentId).ToList();

    public (Personnel personnel, List<PersonnelAccess> accesses) GetWithAccesses(Guid personnelId)
    {
        var p = GetById(personnelId);
        if (p == null) return (null!, new List<PersonnelAccess>());
        var accesses = _store.PersonnelAccesses.Where(a => a.PersonnelId == personnelId && a.IsActive).ToList();
        return (p, accesses);
    }

    public Personnel Add(Personnel personnel)
    {
        personnel.Id = Guid.NewGuid();
        _store.Personnel.Add(personnel);
        return personnel;
    }

    public void Update(Personnel personnel)
    {
        var idx = _store.Personnel.FindIndex(p => p.Id == personnel.Id);
        if (idx >= 0) _store.Personnel[idx] = personnel;
    }

    public void SetOffboarded(Guid personnelId, DateTime endDate)
    {
        var p = GetById(personnelId);
        if (p == null) return;
        p.EndDate = endDate;
        p.Status = PersonnelStatus.Offboarded;
        foreach (var a in _store.PersonnelAccesses.Where(x => x.PersonnelId == personnelId))
            a.IsActive = false;
    }
}
