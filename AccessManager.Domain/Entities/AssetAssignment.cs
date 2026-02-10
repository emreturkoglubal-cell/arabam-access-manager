namespace AccessManager.Domain.Entities;

/// <summary>Zimmet kaydı: donanımın personele ne zaman verildiği / ne zaman iade edildiği.</summary>
public class AssetAssignment
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public Guid PersonnelId { get; set; }
    public DateTime AssignedAt { get; set; }
    public Guid? AssignedByUserId { get; set; }
    public string? AssignedByUserName { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public string? ReturnCondition { get; set; }
    public string? Notes { get; set; }

    public Asset? Asset { get; set; }
    public Personnel? Personnel { get; set; }
}
