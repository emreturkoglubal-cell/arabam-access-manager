using AccessManager.Domain.Enums;

namespace AccessManager.Domain.Entities;

/// <summary>
/// Kaynak sistem (erişim verilebilecek uygulama, altyapı veya lisans). Ad, kod, tür, kritiklik, sorumlu departman ve sorumlu kişi (OwnerId) bilgilerini tutar.
/// </summary>
public class ResourceSystem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public SystemType SystemType { get; set; }
    public CriticalLevel CriticalLevel { get; set; }
    /// <summary>Sorumlu departman.</summary>
    public int? ResponsibleDepartmentId { get; set; }
    /// <summary>Sorumlu kişi (personel).</summary>
    public int? OwnerId { get; set; }
    public string? Description { get; set; }

    public Department? ResponsibleDepartment { get; set; }
    public Personnel? Owner { get; set; }
}
