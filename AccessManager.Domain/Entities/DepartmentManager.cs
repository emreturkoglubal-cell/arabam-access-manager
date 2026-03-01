namespace AccessManager.Domain.Entities;

/// <summary>Departman 1./2./3. yöneticisi; birden fazla kişi atanabilir.</summary>
public class DepartmentManager
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public int PersonnelId { get; set; }
    /// <summary>1=1. Yönetici, 2=2. Yönetici, 3=3. Yönetici.</summary>
    public short ManagerLevel { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }

    public Department? Department { get; set; }
    public Personnel? Personnel { get; set; }
}
