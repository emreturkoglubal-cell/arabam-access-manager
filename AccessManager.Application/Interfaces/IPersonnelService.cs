using AccessManager.Domain.Entities;

namespace AccessManager.Application.Interfaces;

public interface IPersonnelService
{
    IReadOnlyList<Personnel> GetAll();
    IReadOnlyList<Personnel> GetActive();
    Personnel? GetById(int id);
    Personnel? GetBySicilNo(string sicilNo);
    IReadOnlyList<Personnel> GetByManagerId(int managerId);
    IReadOnlyList<Personnel> GetByDepartmentId(int departmentId);
    (Personnel? personnel, List<PersonnelAccess> accesses) GetWithAccesses(int personnelId);
    Personnel Add(Personnel personnel);
    void Update(Personnel personnel);
    void SetOffboarded(int personnelId, DateTime endDate);
    /// <summary>Personelin 10 üzerinden puanını ve yönetici yorumunu günceller (tek değer, birikmez).</summary>
    void UpdateRating(int personnelId, decimal? rating, string? managerComment);
    /// <summary>Faz 1: Personel notlarını getirir (birden fazla not, kim yazdığı görünür).</summary>
    IReadOnlyList<PersonnelNote> GetNotes(int personnelId);
    /// <summary>Faz 1: Personel için yeni not ekler.</summary>
    void AddNote(int personnelId, string content, int? createdByUserId, string? createdByUserName);
}
