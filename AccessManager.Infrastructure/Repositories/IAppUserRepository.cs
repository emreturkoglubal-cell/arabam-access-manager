using AccessManager.Domain.Entities;

namespace AccessManager.Infrastructure.Repositories;

public interface IAppUserRepository
{
    AppUser? GetByUserName(string userName);
    AppUser? ValidateUser(string userName, string passwordHashOrPlain);
    string? GetPersonnelImageUrlByPersonnelId(int? personnelId);
}
