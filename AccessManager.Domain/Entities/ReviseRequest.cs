using AccessManager.Domain.Enums;

namespace AccessManager.Domain.Entities;

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
