using AccessManager.Domain.Entities;
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
        const string sql = @"SELECT id AS Id, first_name AS FirstName, last_name AS LastName, email AS Email,
            department_id AS DepartmentId, position AS Position, manager_id AS ManagerId, start_date AS StartDate, end_date AS EndDate,
            status AS Status, role_id AS RoleId, location AS Location, image_url AS ImageUrl, rating AS Rating, manager_comment AS ManagerComment
            FROM personnel ORDER BY status, id";
        return conn.Query<Personnel>(sql).ToList();
    }

    public IReadOnlyList<Personnel> GetActive()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, first_name AS FirstName, last_name AS LastName, email AS Email,
            department_id AS DepartmentId, position AS Position, manager_id AS ManagerId, start_date AS StartDate, end_date AS EndDate,
            status AS Status, role_id AS RoleId, location AS Location, image_url AS ImageUrl, rating AS Rating, manager_comment AS ManagerComment
            FROM personnel WHERE status = 0 ORDER BY id";
        return conn.Query<Personnel>(sql).ToList();
    }

    public (IReadOnlyList<Personnel> Items, int TotalCount) GetPaged(int? departmentId, bool activeOnly, string? search, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        var conditions = new List<string> { "1=1" };
        if (activeOnly) conditions.Add("status = 0");
        if (departmentId.HasValue) conditions.Add("department_id = @DepartmentId");
        int? searchId = null;
        string? namePattern = null;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            if (s.Length > 0 && s.All(char.IsDigit))
            {
                searchId = int.Parse(s);
                conditions.Add("id = @SearchId");
            }
            else
            {
                namePattern = "%" + s + "%";
                conditions.Add("(first_name ILIKE @NamePattern OR last_name ILIKE @NamePattern)");
            }
        }
        var where = string.Join(" AND ", conditions);
        var baseSql = $"FROM personnel WHERE {where}";
        var offset = (page - 1) * pageSize;
        var pars = new { DepartmentId = departmentId, SearchId = searchId, NamePattern = namePattern, PageSize = pageSize, Offset = offset };
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        var countSql = $"SELECT COUNT(*) {baseSql}";
        var totalCount = Convert.ToInt32(conn.ExecuteScalar(countSql, pars));
        var dataSql = $@"SELECT id AS Id, first_name AS FirstName, last_name AS LastName, email AS Email,
            department_id AS DepartmentId, position AS Position, manager_id AS ManagerId, start_date AS StartDate, end_date AS EndDate,
            status AS Status, role_id AS RoleId, location AS Location, image_url AS ImageUrl, rating AS Rating, manager_comment AS ManagerComment
            {baseSql} ORDER BY status, id LIMIT @PageSize OFFSET @Offset";
        var items = conn.Query<Personnel>(dataSql, pars).ToList();
        return (items, totalCount);
    }

    public Personnel? GetById(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, first_name AS FirstName, last_name AS LastName, email AS Email,
            department_id AS DepartmentId, position AS Position, manager_id AS ManagerId, start_date AS StartDate, end_date AS EndDate,
            status AS Status, role_id AS RoleId, location AS Location, image_url AS ImageUrl, rating AS Rating, manager_comment AS ManagerComment
            FROM personnel WHERE id = @Id";
        return conn.QuerySingleOrDefault<Personnel>(sql, new { Id = id });
    }

    public IReadOnlyList<Personnel> GetByIds(IReadOnlyList<int> ids)
    {
        if (ids == null || ids.Count == 0)
            return new List<Personnel>();
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, first_name AS FirstName, last_name AS LastName, email AS Email,
            department_id AS DepartmentId, position AS Position, manager_id AS ManagerId, start_date AS StartDate, end_date AS EndDate,
            status AS Status, role_id AS RoleId, location AS Location, image_url AS ImageUrl, rating AS Rating, manager_comment AS ManagerComment
            FROM personnel WHERE id = ANY(@Ids)";
        return conn.Query<Personnel>(sql, new { Ids = ids.Distinct().ToList() }).ToList();
    }

    public IReadOnlyList<Personnel> GetByManagerId(int managerId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, first_name AS FirstName, last_name AS LastName, email AS Email,
            department_id AS DepartmentId, position AS Position, manager_id AS ManagerId, start_date AS StartDate, end_date AS EndDate,
            status AS Status, role_id AS RoleId, location AS Location, image_url AS ImageUrl, rating AS Rating, manager_comment AS ManagerComment
            FROM personnel WHERE manager_id = @ManagerId ORDER BY id";
        return conn.Query<Personnel>(sql, new { ManagerId = managerId }).ToList();
    }

    public IReadOnlyList<Personnel> GetByDepartmentId(int departmentId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"SELECT id AS Id, first_name AS FirstName, last_name AS LastName, email AS Email,
            department_id AS DepartmentId, position AS Position, manager_id AS ManagerId, start_date AS StartDate, end_date AS EndDate,
            status AS Status, role_id AS RoleId, location AS Location, image_url AS ImageUrl, rating AS Rating, manager_comment AS ManagerComment
            FROM personnel WHERE department_id = @DepartmentId ORDER BY id";
        return conn.Query<Personnel>(sql, new { DepartmentId = departmentId }).ToList();
    }

    public IReadOnlyDictionary<int, int> GetPersonnelCountByDepartment()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = "SELECT department_id AS DepartmentId, COUNT(*) AS Cnt FROM personnel GROUP BY department_id";
        var rows = conn.Query<(int DepartmentId, int Cnt)>(sql);
        return rows.ToDictionary(r => r.DepartmentId, r => r.Cnt);
    }

    public int Insert(Personnel personnel)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"INSERT INTO personnel (first_name, last_name, email, department_id, position, manager_id, start_date, end_date, status, role_id, location, image_url, rating, manager_comment)
            VALUES (@FirstName, @LastName, @Email, @DepartmentId, @Position, @ManagerId, @StartDate, @EndDate, @Status, @RoleId, @Location, @ImageUrl, @Rating, @ManagerComment) RETURNING id";
        return conn.ExecuteScalar<int>(sql, new {
            personnel.FirstName, personnel.LastName, personnel.Email, personnel.DepartmentId, personnel.Position,
            personnel.ManagerId, personnel.StartDate, personnel.EndDate, Status = (short)personnel.Status, personnel.RoleId,
            personnel.Location, personnel.ImageUrl, personnel.Rating, personnel.ManagerComment
        });
    }

    public void Update(Personnel personnel)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        const string sql = @"UPDATE personnel SET first_name=@FirstName, last_name=@LastName, email=@Email,
            department_id=@DepartmentId, position=@Position, manager_id=@ManagerId, start_date=@StartDate, end_date=@EndDate,
            status=@Status, role_id=@RoleId, location=@Location, image_url=@ImageUrl, rating=@Rating, manager_comment=@ManagerComment, updated_at=now()
            WHERE id=@Id";
        conn.Execute(sql, new {
            personnel.Id, personnel.FirstName, personnel.LastName, personnel.Email, personnel.DepartmentId, personnel.Position,
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
