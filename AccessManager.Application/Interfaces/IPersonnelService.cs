using AccessManager.Application.Dtos;
using AccessManager.Domain.Entities;

namespace AccessManager.Application.Interfaces;

/// <summary>
/// Personel yönetimi: listeleme (tümü, aktif, sayfalı, departman/yönetici bazlı), ekleme, güncelleme, işten çıkış (SetOffboarded), puan/yorum ve notlar.
/// </summary>
public interface IPersonnelService
{
    /// <summary>Tüm personelleri döner.</summary>
    IReadOnlyList<Personnel> GetAll();
    /// <summary>Durumu Active olan personelleri döner.</summary>
    IReadOnlyList<Personnel> GetActive();
    /// <summary>Departman, aktiflik ve arama metni ile sayfalı personel listesi.</summary>
    PagedResult<Personnel> GetPaged(int? departmentId, bool activeOnly, string? search, int page, int pageSize);
    /// <summary>ID ile tek personel; yoksa null.</summary>
    Personnel? GetById(int id);
    /// <summary>Birden fazla ID için personel listesi (sözlük benzeri kullanım için).</summary>
    IReadOnlyList<Personnel> GetByIds(IReadOnlyList<int> ids);
    /// <summary>Belirtilen yöneticiye (ManagerId) bağlı personeller.</summary>
    IReadOnlyList<Personnel> GetByManagerId(int managerId);
    /// <summary>Belirtilen departmandaki personeller.</summary>
    IReadOnlyList<Personnel> GetByDepartmentId(int departmentId);
    /// <summary>Her departman ID'si için personel sayısı (raporlama için).</summary>
    IReadOnlyDictionary<int, int> GetPersonnelCountByDepartment();
    /// <summary>Personel ve o personelin tüm erişim kayıtlarını (PersonnelAccess) birlikte döner.</summary>
    (Personnel? personnel, List<PersonnelAccess> accesses) GetWithAccesses(int personnelId);
    /// <summary>Yeni personel ekler; eklenen entity döner.</summary>
    Personnel Add(Personnel personnel);
    /// <summary>Mevcut personel bilgilerini günceller.</summary>
    void Update(Personnel personnel);
    /// <summary>Personeli işten çıkış yapar: Status = Offboarded, EndDate kaydedilir.</summary>
    void SetOffboarded(int personnelId, DateTime endDate);
    /// <summary>Personelin 10 üzerinden puanını ve yönetici yorumunu günceller (tek değer, birikmez).</summary>
    void UpdateRating(int personnelId, decimal? rating, string? managerComment);
    /// <summary>Faz 1: Personel notlarını getirir (birden fazla not, kim yazdığı görünür).</summary>
    IReadOnlyList<PersonnelNote> GetNotes(int personnelId);
    /// <summary>Faz 1: Personel için yeni not ekler.</summary>
    void AddNote(int personnelId, string content, int? createdByUserId, string? createdByUserName);
}
