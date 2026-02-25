using AccessManager.Domain.Entities;

namespace AccessManager.UI.ViewModels;

public class PersonnelIndexViewModel
{
    public IReadOnlyList<Personnel> PersonnelList { get; set; } = new List<Personnel>();
    public IReadOnlyList<Department> Departments { get; set; } = new List<Department>();
    public IReadOnlyList<Role> Roles { get; set; } = new List<Role>();
    public IReadOnlyDictionary<int, string> DepartmentNames { get; set; } = new Dictionary<int, string>();
    public IReadOnlyDictionary<int, string> RoleNames { get; set; } = new Dictionary<int, string>();
    public Dictionary<int, string> ManagerNames { get; set; } = new();
    public string? SearchTerm { get; set; }
    public int? FilterDepartmentId { get; set; }
    /// <summary>all = tümü, active = sadece aktif, offboarded = işten çıkanlar</summary>
    public string FilterStatusFilter { get; set; } = "all";
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
