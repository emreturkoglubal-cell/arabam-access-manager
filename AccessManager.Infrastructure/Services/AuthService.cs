using AccessManager.Application.Interfaces;
using AccessManager.Domain.Entities;
using AccessManager.Infrastructure.Repositories;

namespace AccessManager.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IAppUserRepository _repo;

    public AuthService(IAppUserRepository repo)
    {
        _repo = repo;
    }

    public AppUser? ValidateUser(string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            return null;
        return _repo.ValidateUser(userName, password);
    }

    public string? GetPersonnelPhotoUrlByUsername(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName)) return null;
        var user = _repo.GetByUserName(userName);
        if (user?.PersonnelId == null) return null;
        return _repo.GetPersonnelImageUrlByPersonnelId(user.PersonnelId);
    }
}
