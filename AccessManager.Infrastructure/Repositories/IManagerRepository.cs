using AccessManager.Domain.Entities;

namespace AccessManager.Infrastructure.Repositories;

public interface IManagerRepository
{
    /// <summary>Hiyerarşide altında başka yönetici olmayan (en alt) yöneticileri döner.</summary>
    IReadOnlyList<Manager> GetLeafManagers();
    /// <summary>is_active = true olan tüm yöneticileri level, id sırasıyla döner.</summary>
    IReadOnlyList<Manager> GetActiveManagers();
    Manager? GetById(int id);
    Manager? GetByPersonnelId(int personnelId);
    IReadOnlyList<Manager> GetAll();
    int Insert(Manager manager);
    void Update(Manager manager);
    void Delete(int id);
}
