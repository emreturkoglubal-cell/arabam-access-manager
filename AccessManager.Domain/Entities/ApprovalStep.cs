namespace AccessManager.Domain.Entities;

public class ApprovalStep
{
    public Guid Id { get; set; }
    public Guid AccessRequestId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public Guid? ApprovedBy { get; set; }
    /// <summary>Onaylayan kullanıcının görünen adı (giriş yapan kullanıcı). Personel kaydı yoksa da doğru isim gösterilir.</summary>
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool? Approved { get; set; }
    public string? Comment { get; set; }
    public int Order { get; set; }

    public AccessRequest? AccessRequest { get; set; }
    public Personnel? Approver { get; set; }
}
