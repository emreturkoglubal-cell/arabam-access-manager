using AccessManager.Domain.Enums;

namespace AccessManager.Domain.Entities;

/// <summary>
/// Uygulama girişi için kullanıcı (kimlik doğrulama). Personnel ile ayrı.
/// </summary>
public class AppUser
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Mock ortamda basit karşılaştırma için; gerçek ortamda hash kullanılmalı.</summary>
    public string PasswordHash { get; set; } = string.Empty;
    public AppRole Role { get; set; }
    /// <summary>Girişte yüz doğrulama için personel fotoğrafı kullanılır.</summary>
    public Guid? PersonnelId { get; set; }
}
