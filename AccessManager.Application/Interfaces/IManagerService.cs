using AccessManager.Domain.Entities;

namespace AccessManager.Application.Interfaces;

/// <summary>
/// Yönetici hiyerarşisi: personel formunda sadece en alt yöneticiler (leaf) listelenir; detayda yönetici ve seviye atanabilir.
/// </summary>
public interface IManagerService
{
    /// <summary>Personel eklerken Yönetici dropdown'ında gösterilecek kişiler: managers tablosundaki en alt yöneticilerin personel kayıtları. Tablo boşsa tüm aktif personel döner.</summary>
    IReadOnlyList<Personnel> GetLeafManagerPersonnel();
    /// <summary>Belirtilen personel ID'sinin managers kaydındaki seviyesini (1-4) döner; yoksa null.</summary>
    short? GetManagerLevelByPersonnelId(int personnelId);
    /// <summary>Personelin yöneticisini ve (yönetici atanmışsa) o yöneticinin seviyesini günceller.</summary>
    void UpdatePersonnelManager(int personnelId, int? managerPersonnelId, short level);
    /// <summary>Personeli yönetici olarak işaretler veya pasife alır. isManager true ise: managerPersonnelId = personelin yöneticisi (formdan); parent_manager_id = o yöneticinin managers.id, level = o yöneticinin level + 1 (max 4); yönetici yoksa level=1, parent=null. false ise is_active = false.</summary>
    void SetPersonAsManager(int personnelId, bool isManager, int? managerPersonnelId = null);
    /// <summary>Personelin managers kaydı var mı ve aktif mi döner.</summary>
    bool IsPersonManagerActive(int personnelId);
}
