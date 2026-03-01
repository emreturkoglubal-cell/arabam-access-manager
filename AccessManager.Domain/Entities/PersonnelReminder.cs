namespace AccessManager.Domain.Entities;

/// <summary>Personel hatırlatması; reminder_date günü mail atılır.</summary>
public class PersonnelReminder
{
    public int Id { get; set; }
    public int PersonnelId { get; set; }
    public DateTime ReminderDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }

    public Personnel? Personnel { get; set; }
}
