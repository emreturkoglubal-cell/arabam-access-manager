using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Infrastructure.Data;

namespace AccessManager.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly MockDataStore _store;

    public AuthService(MockDataStore store)
    {
        _store = store;
    }

    public AppUser? ValidateUser(string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            return null;

        // Mock: parola düz metin karşılaştırma (gerçek ortamda hash kullanılmalı)
        var user = _store.AppUsers.Find(u =>
            string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase) &&
            u.PasswordHash == password);
        return user;
    }

    public string? GetPersonnelPhotoUrlByUsername(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName)) return null;
        var user = _store.AppUsers.Find(u => string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase));
        if (user?.PersonnelId == null) return null;
        var personnel = _store.Personnel.Find(p => p.Id == user.PersonnelId);
        return string.IsNullOrWhiteSpace(personnel?.ImageUrl) ? null : personnel.ImageUrl;
    }
}
