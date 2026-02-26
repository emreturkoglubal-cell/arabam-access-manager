namespace AccessManager.Domain.Entities;

public class AiConversation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
