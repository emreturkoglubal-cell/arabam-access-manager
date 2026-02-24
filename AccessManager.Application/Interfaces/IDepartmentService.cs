using AccessManager.Domain.Entities;

namespace AccessManager.Application.Interfaces;

/// <summary>
/// Departman yönetimi: listeleme ve yeni departman ekleme. Personeller bir departmana (DepartmentId) bağlıdır.
/// </summary>
public interface IDepartmentService
{
    /// <summary>Tüm departmanları döner.</summary>
    IReadOnlyList<Department> GetAll();
    /// <summary>ID ile tek departman; yoksa null.</summary>
    Department? GetById(int id);
    /// <summary>Yeni departman oluşturur; ad zorunlu, kod ve açıklama isteğe bağlı.</summary>
    Department Add(string name, string? code, string? description);
    /// <summary>Mevcut departman bilgilerini günceller.</summary>
    void Update(Department department);
}
