namespace AccessManager.Domain.Entities;

/// <summary>
/// Genişletilmiş log kaydı: hata/bilgi, IP, URL, kullanıcı vb.
/// </summary>
public class ExtendedLog
{
    public int Id { get; set; }
    public string Level { get; set; } = "Error";
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? IpAddress { get; set; }
    public string? Url { get; set; }
    public string? HttpMethod { get; set; }
    public string? UserAgent { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ExtraData { get; set; }
}
