using AccessManager.Domain.Entities;

namespace AccessManager.Infrastructure.Repositories;

public interface IManagerRepository
{
    /// <summary>Hiyerarşide altında başka yönetici olmayan (en alt) yöneticileri döner. Personel formu dropdown için.</summary>
    IReadOnlyList<Manager> GetLeafManagers();
    Manager? GetById(int id);
    Manager? GetByPersonnelId(int personnelId);
    IReadOnlyList<Manager> GetAll();
    int Insert(Manager manager);
    void Update(Manager manager);
    void Delete(int id);
}
