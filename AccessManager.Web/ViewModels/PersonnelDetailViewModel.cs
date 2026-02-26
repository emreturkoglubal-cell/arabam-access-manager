using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.UI.ViewModels;

public class PersonnelDetailViewModel
{
    public Personnel? Personnel { get; set; }
    public List<PersonnelAccess> AccessList { get; set; } = new();
    public List<AssetAssignment> AssetAssignments { get; set; } = new();
    public Dictionary<int, string> AssetNames { get; set; } = new();
    public Dictionary<int, AssetType> AssetTypes { get; set; } = new();
    public string? DepartmentName { get; set; }
    public string? RoleName { get; set; }
    public string? ManagerName { get; set; }
    public Dictionary<int, string> SystemNames { get; set; } = new();
    /// <summary>Sistem id -> sorumlu kişi adı (yetkiler listesinde link için). Tekil kullanım için geriye uyumluluk; çoklu için SystemOwnersList kullanın.</summary>
    public Dictionary<int, string> SystemOwnerNames { get; set; } = new();
    /// <summary>Sistem id -> sorumlu kişiler listesi (PersonnelId, Name); yetkiler tablosunda birden fazla link için.</summary>
    public Dictionary<int, List<(int PersonnelId, string Name)>> SystemOwnersList { get; set; } = new();
    /// <summary>Sistem id -> sorumlu departman adı (yetkiler listesinde link için).</summary>
    public Dictionary<int, string> SystemResponsibleDepartmentNames { get; set; } = new();
    /// <summary>Tüm uygulamalar (yetkisi olan ve olmayan görünsün).</summary>
    public List<ResourceSystem> AllSystems { get; set; } = new();
    /// <summary>Bu personelin aktif uygulamalarının toplam maliyeti (USD cinsinden; diğer para birimleri kur ile çevrilir).</summary>
    public decimal? ApplicationCostUsd { get; set; }
    /// <summary>Faz 1: Birden fazla not, kim yazdığı görünsün.</summary>
    public List<PersonnelNote> Notes { get; set; } = new();
    /// <summary>Zimmet kaydına göre notlar (AssignmentId -> not listesi).</summary>
    public Dictionary<int, List<AssetAssignmentNote>> AssignmentNotes { get; set; } = new();
    /// <summary>Bu personel yönetici ise, ona bağlı personeller (sayfalı).</summary>
    public IReadOnlyList<Personnel> Subordinates { get; set; } = new List<Personnel>();
    public int SubordinatesTotalCount { get; set; }
    public int SubordinatesPage { get; set; } = 1;
    public int SubordinatesPageSize { get; set; } = 10;
}
