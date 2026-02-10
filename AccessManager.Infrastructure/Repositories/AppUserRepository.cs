using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class AppUserRepository : IAppUserRepository
{
    private readonly string _connectionString;

    public AppUserRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public AppUser? GetByUserName(string userName)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, user_name AS UserName, display_name AS DisplayName, password_hash AS PasswordHash,
            role AS Role, personnel_id AS PersonnelId FROM app_users WHERE LOWER(user_name) = LOWER(@UserName)";
        var row = conn.QuerySingleOrDefault<AppUserRow>(sql, new { UserName = userName });
        return row == null ? null : new AppUser
        {
            Id = row.Id,
            UserName = row.UserName,
            DisplayName = row.DisplayName,
            PasswordHash = row.PasswordHash,
            Role = (AppRole)row.Role,
            PersonnelId = row.PersonnelId
        };
    }

    public AppUser? ValidateUser(string userName, string passwordHashOrPlain)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, user_name AS UserName, display_name AS DisplayName, password_hash AS PasswordHash,
            role AS Role, personnel_id AS PersonnelId FROM app_users WHERE LOWER(user_name) = LOWER(@UserName) AND password_hash = @Password";
        var row = conn.QuerySingleOrDefault<AppUserRow>(sql, new { UserName = userName, Password = passwordHashOrPlain });
        return row == null ? null : new AppUser
        {
            Id = row.Id,
            UserName = row.UserName,
            DisplayName = row.DisplayName,
            PasswordHash = row.PasswordHash,
            Role = (AppRole)row.Role,
            PersonnelId = row.PersonnelId
        };
    }

    public string? GetPersonnelImageUrlByPersonnelId(int? personnelId)
    {
        if (!personnelId.HasValue) return null;
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        return conn.ExecuteScalar<string?>("SELECT image_url FROM personnel WHERE id = @Id", new { Id = personnelId.Value });
    }

    private class AppUserRow
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public short Role { get; set; }
        public int? PersonnelId { get; set; }
    }
}
