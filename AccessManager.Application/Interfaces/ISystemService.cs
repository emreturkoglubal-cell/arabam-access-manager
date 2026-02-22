using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.Application.Interfaces;

/// <summary>
/// Kaynak sistem (ResourceSystem) yönetimi: CRUD, tür/kritiklik ile listeleme. Sistemler erişim talebi, rol yetkisi ve personel erişiminde kullanılır.
/// </summary>
public interface ISystemService
{
    /// <summary>Tüm kaynak sistemlerini döner.</summary>
    IReadOnlyList<ResourceSystem> GetAll();
    /// <summary>ID ile tek sistem; yoksa null.</summary>
    ResourceSystem? GetById(int id);
    /// <summary>Birden fazla ID için sistem listesi.</summary>
    IReadOnlyList<ResourceSystem> GetByIds(IReadOnlyList<int> ids);
    /// <summary>Belirtilen türe (Application, Infrastructure, License) göre sistemler.</summary>
    IReadOnlyList<ResourceSystem> GetByType(SystemType type);
    /// <summary>Belirtilen kritiklik seviyesine göre sistemler.</summary>
    IReadOnlyList<ResourceSystem> GetByCriticalLevel(CriticalLevel level);

    /// <summary>Yeni kaynak sistem oluşturur.</summary>
    ResourceSystem Create(ResourceSystem system);
    /// <summary>Mevcut sistem bilgilerini günceller.</summary>
    void Update(ResourceSystem system);
    /// <summary>Siler. Yetki talebi, rol yetkisi veya personel erişiminde kullanılıyorsa false döner.</summary>
    bool Delete(int id);
}
