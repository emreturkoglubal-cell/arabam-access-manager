using AccessManager.Models;

namespace AccessManager.Services;

public interface IPersonnelService
{
    IReadOnlyList<Personnel> GetAll();
    IReadOnlyList<Personnel> GetActive();
    Personnel? GetById(Guid id);
    Personnel? GetBySicilNo(string sicilNo);
    IReadOnlyList<Personnel> GetByManagerId(Guid managerId);
    IReadOnlyList<Personnel> GetByDepartmentId(Guid departmentId);
    (Personnel personnel, List<PersonnelAccess> accesses) GetWithAccesses(Guid personnelId);
    Personnel Add(Personnel personnel);
    void Update(Personnel personnel);
    void SetOffboarded(Guid personnelId, DateTime endDate);
}
