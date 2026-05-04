using AccessManager.Domain.Entities;

namespace AccessManager.Infrastructure.Repositories;

public interface IPersonnelRepository
{
    IReadOnlyList<Personnel> GetAll();
    IReadOnlyList<Personnel> GetActive();
    (IReadOnlyList<Personnel> Items, int TotalCount) GetPaged(int? departmentId, int? roleId, string? statusFilter, string? search, int page, int pageSize);
    Personnel? GetById(int id);
    IReadOnlyList<Personnel> GetByIds(IReadOnlyList<int> ids);
    IReadOnlyList<Personnel> GetByManagerId(int managerId);
    IReadOnlyList<Personnel> GetByDepartmentId(int departmentId);
    IReadOnlyDictionary<int, int> GetPersonnelCountByDepartment();
    IReadOnlyDictionary<int, int> GetPersonnelCountByRole();
    int Insert(Personnel personnel);
    void Update(Personnel personnel);
    void SetOffboarded(int personnelId, DateTime endDate);
    void UpdateRating(int personnelId, decimal? rating, string? managerComment);
    IReadOnlyList<PersonnelNote> GetNotes(int personnelId);
    void AddNote(PersonnelNote note);
    /// <summary>İşe giriş tarihi aralığında (dahil) personel; departman filtresi isteğe bağlı.</summary>
    IReadOnlyList<Personnel> GetByStartDateInRange(DateTime fromInclusive, DateTime toInclusive, int? departmentId);
    /// <summary>İşten çıkış tarihi aralığında (dahil) ve status Offboarded personel.</summary>
    IReadOnlyList<Personnel> GetByEndDateOffboardedInRange(DateTime fromInclusive, DateTime toInclusive, int? departmentId);
}
