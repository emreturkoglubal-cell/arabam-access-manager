using AccessManager.Domain.Entities;

namespace AccessManager.Infrastructure.Repositories;

public interface IExtendedLogRepository
{
    void Insert(ExtendedLog log);
}
