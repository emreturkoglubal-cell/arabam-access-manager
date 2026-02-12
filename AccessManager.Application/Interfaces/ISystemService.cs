using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.Application.Interfaces;

public interface ISystemService
{
    IReadOnlyList<ResourceSystem> GetAll();
    ResourceSystem? GetById(int id);
    IReadOnlyList<ResourceSystem> GetByIds(IReadOnlyList<int> ids);
    IReadOnlyList<ResourceSystem> GetByType(SystemType type);
    IReadOnlyList<ResourceSystem> GetByCriticalLevel(CriticalLevel level);

    ResourceSystem Create(ResourceSystem system);
    void Update(ResourceSystem system);
    /// <summary>Siler. Yetki talebi, rol yetkisi veya personel erişiminde kullanılıyorsa false döner.</summary>
    bool Delete(int id);
}
