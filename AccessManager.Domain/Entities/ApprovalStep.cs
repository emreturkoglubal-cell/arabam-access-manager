namespace AccessManager.Domain.Entities;

/// <summary>
/// Erişim talebinin (AccessRequest) bir onay adımı. Yönetici, sistem sahibi veya IT onayı; StepName ile adım adı (örn. Manager, SystemOwner, IT), onaylayan ve tarih bilgisi.
/// </summary>
public class ApprovalStep
{
    public int Id { get; set; }
    public int AccessRequestId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public int? ApprovedBy { get; set; }
    /// <summary>Onaylayan kullanıcının görünen adı (giriş yapan kullanıcı). Personel kaydı yoksa da doğru isim gösterilir.</summary>
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool? Approved { get; set; }
    public string? Comment { get; set; }
    public int Order { get; set; }

    public AccessRequest? AccessRequest { get; set; }
    public Personnel? Approver { get; set; }
}
