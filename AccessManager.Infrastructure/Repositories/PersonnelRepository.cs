using AccessManager.Domain.Entities;
using AccessManager.Domain.Enums;
using Dapper;
using Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class PersonnelRepository : IPersonnelRepository
{
    private readonly string _connectionString;

    public PersonnelRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IReadOnlyList<Personnel> GetAll()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, sicil_no AS SicilNo, first_name AS FirstName, last_name AS LastName, email AS Email,
            department_id AS DepartmentId, position AS Position, manager_id AS ManagerId, start_date AS StartDate, end_date AS EndDate,
            status AS Status, role_id AS RoleId, location AS Location, image_url AS ImageUrl, rating AS Rating, manager_comment AS ManagerComment
            FROM personnel ORDER BY sicil_no";
        return conn.Query<Personnel>(sql).ToList();
    }

    public IReadOnlyList<Personnel> GetActive()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, sicil_no AS SicilNo, first_name AS FirstName, last_name AS LastName, email AS Email,
            department_id AS DepartmentId, position AS Position, manager_id AS ManagerId, start_date AS StartDate, end_date AS EndDate,
            status AS Status, role_id AS RoleId, location AS Location, image_url AS ImageUrl, rating AS Rating, manager_comment AS ManagerComment
            FROM personnel WHERE status = 0 ORDER BY sicil_no";
        return conn.Query<Personnel>(sql).ToList();
    }

    public (IReadOnlyList<Personnel> Items, int TotalCount) GetPaged(int? departmentId, bool activeOnly, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        var conditions = new List<string> { "1=1" };
        if (activeOnly) conditions.Add("status = 0");
        if (departmentId.HasValue) conditions.Add("department_id = @DepartmentId");
        var where = string.Join(" AND ", conditions);
        var baseSql = $"FROM personnel WHERE {where}";
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        var countSql = $"SELECT COUNT(*) {baseSql}";
        var totalCount = conn.ExecuteScalar<int>(countSql, new { DepartmentId = departmentId });
        var offset = (page - 1) * pageSize;
        var dataSql = $@"SELECT id AS Id, sicil_no AS SicilNo, first_name AS FirstName, last_name AS LastName, email AS Email,
            department_id AS DepartmentId, position AS Position, manager_id AS ManagerId, start_date AS StartDate, end_date AS EndDate,
            status AS Status, role_id AS RoleId, location AS Location, image_url AS ImageUrl, rating AS Rating, manager_comment AS ManagerComment
            {baseSql} ORDER BY sicil_no LIMIT @PageSize OFFSET @Offset";
        var items = conn.Query<Personnel>(dataSql, new { DepartmentId = departmentId, PageSize = pageSize, Offset = offset }).ToList();
        return (items, totalCount);
    }

    public Personnel? GetById(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, sicil_no AS SicilNo, first_name AS FirstName, last_name AS LastName, email AS Email,
            department_id AS DepartmentId, position AS Position, manager_id AS ManagerId, start_date AS StartDate, end_date AS EndDate,
            status AS Status, role_id AS RoleId, location AS Location, image_url AS ImageUrl, rating AS Rating, manager_comment AS ManagerComment
            FROM personnel WHERE id = @Id";
        return conn.QuerySingleOrDefault<Personnel>(sql, new { Id = id });
    }

    public Personnel? GetBySicilNo(string sicilNo)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, sicil_no AS SicilNo, first_name AS FirstName, last_name AS LastName, email AS Email,
            department_id AS DepartmentId, position AS Position, manager_id AS ManagerId, start_date AS StartDate, end_date AS EndDate,
            status AS Status, role_id AS RoleId, location AS Location, image_url AS ImageUrl, rating AS Rating, manager_comment AS ManagerComment
            FROM personnel WHERE LOWER(sicil_no) = LOWER(@SicilNo)";
        return conn.QuerySingleOrDefault<Personnel>(sql, new { SicilNo = sicilNo });
    }

    public IReadOnlyList<Personnel> GetByManagerId(int managerId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, sicil_no AS SicilNo, first_name AS FirstName, last_name AS LastName, email AS Email,
            department_id AS DepartmentId, position AS Position, manager_id AS ManagerId, start_date AS StartDate, end_date AS EndDate,
            status AS Status, role_id AS RoleId, location AS Location, image_url AS ImageUrl, rating AS Rating, manager_comment AS ManagerComment
            FROM personnel WHERE manager_id = @ManagerId ORDER BY sicil_no";
        return conn.Query<Personnel>(sql, new { ManagerId = managerId }).ToList();
    }

    public IReadOnlyList<Personnel> GetByDepartmentId(int departmentId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, sicil_no AS SicilNo, first_name AS FirstName, last_name AS LastName, email AS Email,
            department_id AS DepartmentId, position AS Position, manager_id AS ManagerId, start_date AS StartDate, end_date AS EndDate,
            status AS Status, role_id AS RoleId, location AS Location, image_url AS ImageUrl, rating AS Rating, manager_comment AS ManagerComment
            FROM personnel WHERE department_id = @DepartmentId ORDER BY sicil_no";
        return conn.Query<Personnel>(sql, new { DepartmentId = departmentId }).ToList();
    }

    public int Insert(Personnel personnel)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO personnel (sicil_no, first_name, last_name, email, department_id, position, manager_id, start_date, end_date, status, role_id, location, image_url, rating, manager_comment)
            VALUES (@SicilNo, @FirstName, @LastName, @Email, @DepartmentId, @Position, @ManagerId, @StartDate, @EndDate, @Status, @RoleId, @Location, @ImageUrl, @Rating, @ManagerComment) RETURNING id";
        return conn.ExecuteScalar<int>(sql, new {
            personnel.SicilNo, personnel.FirstName, personnel.LastName, personnel.Email, personnel.DepartmentId, personnel.Position,
            personnel.ManagerId, personnel.StartDate, personnel.EndDate, Status = (short)personnel.Status, personnel.RoleId,
            personnel.Location, personnel.ImageUrl, personnel.Rating, personnel.ManagerComment
        });
    }

    public void Update(Personnel personnel)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"UPDATE personnel SET sicil_no=@SicilNo, first_name=@FirstName, last_name=@LastName, email=@Email,
            department_id=@DepartmentId, position=@Position, manager_id=@ManagerId, start_date=@StartDate, end_date=@EndDate,
            status=@Status, role_id=@RoleId, location=@Location, image_url=@ImageUrl, rating=@Rating, manager_comment=@ManagerComment, updated_at=now()
            WHERE id=@Id";
        conn.Execute(sql, new {
            personnel.Id, personnel.SicilNo, personnel.FirstName, personnel.LastName, personnel.Email, personnel.DepartmentId, personnel.Position,
            personnel.ManagerId, personnel.StartDate, personnel.EndDate, Status = (short)personnel.Status, personnel.RoleId,
            personnel.Location, personnel.ImageUrl, personnel.Rating, personnel.ManagerComment
        });
    }

    public void SetOffboarded(int personnelId, DateTime endDate)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        conn.Execute("UPDATE personnel SET end_date = @EndDate, status = 2, updated_at = now() WHERE id = @Id", new { Id = personnelId, EndDate = endDate });
        conn.Execute("UPDATE personnel_accesses SET is_active = false WHERE personnel_id = @PersonnelId", new { PersonnelId = personnelId });
    }

    public void UpdateRating(int personnelId, decimal? rating, string? managerComment)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        conn.Execute("UPDATE personnel SET rating = @Rating, manager_comment = @ManagerComment, updated_at = now() WHERE id = @Id",
            new { Id = personnelId, Rating = rating, ManagerComment = managerComment });
    }

    public IReadOnlyList<PersonnelNote> GetNotes(int personnelId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, personnel_id AS PersonnelId, content AS Content, created_at AS CreatedAt,
            created_by_user_id AS CreatedByUserId, created_by_user_name AS CreatedByUserName
            FROM personnel_notes WHERE personnel_id = @PersonnelId ORDER BY created_at DESC";
        return conn.Query<PersonnelNote>(sql, new { PersonnelId = personnelId }).ToList();
    }

    public void AddNote(PersonnelNote note)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO personnel_notes (personnel_id, content, created_at, created_by_user_id, created_by_user_name)
            VALUES (@PersonnelId, @Content, @CreatedAt, @CreatedByUserId, @CreatedByUserName)";
        conn.Execute(sql, new { note.PersonnelId, note.Content, note.CreatedAt, note.CreatedByUserId, note.CreatedByUserName });
    }
}
