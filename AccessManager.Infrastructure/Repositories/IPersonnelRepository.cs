using AccessManager.Domain.Entities;

namespace AccessManager.Infrastructure.Repositories;

public interface IPersonnelRepository
{
    IReadOnlyList<Personnel> GetAll();
    IReadOnlyList<Personnel> GetActive();
    (IReadOnlyList<Personnel> Items, int TotalCount) GetPaged(int? departmentId, bool activeOnly, string? search, int page, int pageSize);
    Personnel? GetById(int id);
    IReadOnlyList<Personnel> GetByIds(IReadOnlyList<int> ids);
    IReadOnlyList<Personnel> GetByManagerId(int managerId);
    IReadOnlyList<Personnel> GetByDepartmentId(int departmentId);
    IReadOnlyDictionary<int, int> GetPersonnelCountByDepartment();
    int Insert(Personnel personnel);
    void Update(Personnel personnel);
    void SetOffboarded(int personnelId, DateTime endDate);
    void UpdateRating(int personnelId, decimal? rating, string? managerComment);
    IReadOnlyList<PersonnelNote> GetNotes(int personnelId);
    void AddNote(PersonnelNote note);
}
