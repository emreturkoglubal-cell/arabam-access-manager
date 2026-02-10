using AccessManager.Domain.Entities;

namespace AccessManager.Application.Interfaces;

public interface IPersonnelService
{
    IReadOnlyList<Personnel> GetAll();
    IReadOnlyList<Personnel> GetActive();
    Personnel? GetById(Guid id);
    Personnel? GetBySicilNo(string sicilNo);
    IReadOnlyList<Personnel> GetByManagerId(Guid managerId);
    IReadOnlyList<Personnel> GetByDepartmentId(Guid departmentId);
    (Personnel? personnel, List<PersonnelAccess> accesses) GetWithAccesses(Guid personnelId);
    Personnel Add(Personnel personnel);
    void Update(Personnel personnel);
    void SetOffboarded(Guid personnelId, DateTime endDate);
    /// <summary>Personelin 10 üzerinden puanını ve yönetici yorumunu günceller (tek değer, birikmez).</summary>
    void UpdateRating(Guid personnelId, decimal? rating, string? managerComment);
    /// <summary>Faz 1: Personel notlarını getirir (birden fazla not, kim yazdığı görünür).</summary>
    IReadOnlyList<PersonnelNote> GetNotes(Guid personnelId);
    /// <summary>Faz 1: Personel için yeni not ekler.</summary>
    void AddNote(Guid personnelId, string content, Guid? createdByUserId, string? createdByUserName);
}
