using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly string _connectionString;

    public RoleRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IReadOnlyList<Role> GetAll()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, name AS Name, code AS Code, description AS Description FROM roles ORDER BY name";
        return conn.Query<Role>(sql).ToList();
    }

    public Role? GetById(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, name AS Name, code AS Code, description AS Description FROM roles WHERE id = @Id";
        return conn.QuerySingleOrDefault<Role>(sql, new { Id = id });
    }

    public IReadOnlyList<RolePermission> GetPermissionsByRoleId(int roleId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, role_id AS RoleId, resource_system_id AS ResourceSystemId,
            permission_type AS PermissionType, is_default AS IsDefault
            FROM role_permissions WHERE role_id = @RoleId ORDER BY resource_system_id";
        return conn.Query<RolePermission>(sql, new { RoleId = roleId }).ToList();
    }

    public IReadOnlyList<RolePermission> GetAllRolePermissions()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, role_id AS RoleId, resource_system_id AS ResourceSystemId,
            permission_type AS PermissionType, is_default AS IsDefault FROM role_permissions";
        return conn.Query<RolePermission>(sql).ToList();
    }

    public int Insert(Role role)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO roles (name, code, description) VALUES (@Name, @Code, @Description) RETURNING id";
        return conn.ExecuteScalar<int>(sql, new { role.Name, role.Code, role.Description });
    }

    public void Update(Role role)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"UPDATE roles SET name = @Name, code = @Code, description = @Description, updated_at = now() WHERE id = @Id";
        conn.Execute(sql, new { role.Id, role.Name, role.Code, role.Description });
    }

    public bool Delete(int roleId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        conn.Execute("DELETE FROM role_permissions WHERE role_id = @RoleId", new { RoleId = roleId });
        var rows = conn.Execute("DELETE FROM roles WHERE id = @Id", new { Id = roleId });
        return rows > 0;
    }

    public int AddPermission(RolePermission rp)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO role_permissions (role_id, resource_system_id, permission_type, is_default)
            VALUES (@RoleId, @ResourceSystemId, @PermissionType, @IsDefault) RETURNING id";
        return conn.ExecuteScalar<int>(sql, new { rp.RoleId, rp.ResourceSystemId, PermissionType = (short)rp.PermissionType, rp.IsDefault });
    }

    public bool RemovePermission(int rolePermissionId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        var rows = conn.Execute("DELETE FROM role_permissions WHERE id = @Id", new { Id = rolePermissionId });
        return rows > 0;
    }
}
