namespace AccessManager.Domain.Entities;

/// <summary>Personel için birden fazla not; kimin yazdığı takip edilir (Faz 1).</summary>
public class PersonnelNote
{
    public Guid Id { get; set; }
    public Guid PersonnelId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }

    public Personnel? Personnel { get; set; }
}
