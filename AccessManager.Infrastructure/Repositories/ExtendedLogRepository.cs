using AccessManager.Domain.Entities;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class ExtendedLogRepository : IExtendedLogRepository
{
    private readonly string _connectionString;

    public ExtendedLogRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public void Insert(ExtendedLog log)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"
            INSERT INTO extended_logs (level, source, message, exception, ip_address, url, http_method, user_agent, user_id, user_name, created_at, extra_data)
            VALUES (@Level, @Source, @Message, @Exception, @IpAddress, @Url, @HttpMethod, @UserAgent, @UserId, @UserName, COALESCE(@CreatedAt, now()), @ExtraData)";
        conn.Execute(sql, new
        {
            log.Level,
            log.Source,
            log.Message,
            log.Exception,
            log.IpAddress,
            log.Url,
            log.HttpMethod,
            log.UserAgent,
            log.UserId,
            log.UserName,
            CreatedAt = log.CreatedAt == default ? null : (DateTime?)log.CreatedAt,
            log.ExtraData
        });
    }
}
