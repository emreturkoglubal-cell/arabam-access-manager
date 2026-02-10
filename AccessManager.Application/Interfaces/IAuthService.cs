using AccessManager.Domain.Entities;

namespace AccessManager.Application.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Kullanıcı adı ve parola ile doğrular; geçerliyse <see cref="AppUser"/> döner.
    /// </summary>
    AppUser? ValidateUser(string userName, string password);

    /// <summary>
    /// Giriş sayfasında yüz doğrulama için personel fotoğrafı URL'si. Kullanıcı yoksa veya foto yoksa null.
    /// </summary>
    string? GetPersonnelPhotoUrlByUsername(string userName);
}
