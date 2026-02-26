using AccessManager.Domain.Enums;

namespace AccessManager.UI.ViewModels;

public class SystemEditInputModel
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public SystemType SystemType { get; set; } = SystemType.Application;
    public CriticalLevel CriticalLevel { get; set; } = CriticalLevel.Medium;
    /// <summary>0 = seçilmedi.</summary>
    public int ResponsibleDepartmentId { get; set; }
    /// <summary>Sorumlu kişiler (personel id listesi); birden fazla seçilebilir.</summary>
    public List<int> OwnerIds { get; set; } = new();
    public string? Description { get; set; }
}
