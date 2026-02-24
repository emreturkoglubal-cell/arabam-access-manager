namespace AccessManager.Domain.Entities;

/// <summary>
/// Yönetici hiyerarşisi kaydı. Level 1 en üst, 4 en alt. Personel eklerken sadece en alt yönetici (hiyerarşide altında başka yönetici olmayan) seçilir.
/// </summary>
public class Manager
{
    public int Id { get; set; }
    public int PersonnelId { get; set; }
    /// <summary>1 = en üst yönetici, 4 = en alt yönetici.</summary>
    public short Level { get; set; }
    public int? ParentManagerId { get; set; }
    /// <summary>false ise yönetici pasif; dropdown'da listelenmez.</summary>
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Personnel? Personnel { get; set; }
    public Manager? ParentManager { get; set; }
}
