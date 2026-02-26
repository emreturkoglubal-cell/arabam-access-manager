using AccessManager.Domain.Entities;

namespace AccessManager.Infrastructure.Repositories;

public interface ICurrencyRateRepository
{
    IReadOnlyList<CurrencyRate> GetAll();
}
