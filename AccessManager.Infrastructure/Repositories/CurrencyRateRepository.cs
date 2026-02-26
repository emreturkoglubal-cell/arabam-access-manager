using AccessManager.Domain.Entities;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class CurrencyRateRepository : ICurrencyRateRepository
{
    private readonly string _connectionString;

    public CurrencyRateRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IReadOnlyList<CurrencyRate> GetAll()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT code AS Code, rate_to_usd AS RateToUsd FROM currencies ORDER BY code";
        return conn.Query<CurrencyRate>(sql).ToList();
    }
}
