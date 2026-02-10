namespace AccessManager.Domain.Entities;

/// <summary>Zimmet kaydı: donanımın personele ne zaman verildiği / ne zaman iade edildiği.</summary>
public class AssetAssignment
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public int PersonnelId { get; set; }
    public DateTime AssignedAt { get; set; }
    public int? AssignedByUserId { get; set; }
    public string? AssignedByUserName { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public string? ReturnCondition { get; set; }
    public string? Notes { get; set; }

    public Asset? Asset { get; set; }
    public Personnel? Personnel { get; set; }
}
