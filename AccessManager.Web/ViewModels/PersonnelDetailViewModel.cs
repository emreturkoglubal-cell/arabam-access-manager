using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;

namespace AccessManager.UI.ViewModels;

public class PersonnelDetailViewModel
{
    public Personnel? Personnel { get; set; }
    public List<PersonnelAccess> AccessList { get; set; } = new();
    public List<AssetAssignment> AssetAssignments { get; set; } = new();
    public Dictionary<Guid, string> AssetNames { get; set; } = new();
    public Dictionary<Guid, AssetType> AssetTypes { get; set; } = new();
    public string? DepartmentName { get; set; }
    public string? RoleName { get; set; }
    public string? ManagerName { get; set; }
    public Dictionary<Guid, string> SystemNames { get; set; } = new();
    /// <summary>Tüm uygulamalar (yetkisi olan ve olmayan görünsün).</summary>
    public List<ResourceSystem> AllSystems { get; set; } = new();
    /// <summary>Faz 1: Birden fazla not, kim yazdığı görünsün.</summary>
    public List<PersonnelNote> Notes { get; set; } = new();
    /// <summary>Zimmet kaydına göre notlar (AssignmentId -> not listesi).</summary>
    public Dictionary<Guid, List<AssetAssignmentNote>> AssignmentNotes { get; set; } = new();
}
