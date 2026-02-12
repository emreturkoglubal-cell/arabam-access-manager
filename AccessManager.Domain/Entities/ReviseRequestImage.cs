namespace AccessManager.Domain.Entities;

public class ReviseRequestImage
{
    public int Id { get; set; }
    public int ReviseRequestId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? MimeType { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public ReviseRequest? ReviseRequest { get; set; }
}
