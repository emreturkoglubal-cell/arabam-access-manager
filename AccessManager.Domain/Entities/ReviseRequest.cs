using AccessManager.Domain.Enums;

namespace AccessManager.Domain.Entities;

/// <summary>
/// Revizyon / düzeltme talebi. Kullanıcıların sistemle ilgili iyileştirme veya hata bildirimi; başlık, açıklama ve durum (Pending/Resolved). İsteğe bağlı ekran görüntüleri (Images) eklenebilir.
/// </summary>
public class ReviseRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ReviseRequestStatus Status { get; set; } = ReviseRequestStatus.Pending;
    public int? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    // Navigation
    public List<ReviseRequestImage> Images { get; set; } = new();
}
