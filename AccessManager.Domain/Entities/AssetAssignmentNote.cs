namespace AccessManager.Domain.Entities;

/// <summary>Zimmet kaydına eklenen notlar (birden fazla); kimin yazdığı takip edilir.</summary>
public class AssetAssignmentNote
{
    public Guid Id { get; set; }
    public Guid AssetAssignmentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }

    public AssetAssignment? AssetAssignment { get; set; }
}
