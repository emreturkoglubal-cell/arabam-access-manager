using AccessManager.Domain.Entities;

namespace AccessManager.UI.ViewModels;

public class PersonnelIndexViewModel
{
    public IReadOnlyList<Personnel> PersonnelList { get; set; } = new List<Personnel>();
    public IReadOnlyList<Department> Departments { get; set; } = new List<Department>();
    public IReadOnlyList<Role> Roles { get; set; } = new List<Role>();
    public Dictionary<int, string> ManagerNames { get; set; } = new();
    public string? SearchTerm { get; set; }
    public int? FilterDepartmentId { get; set; }
    public bool? FilterActiveOnly { get; set; } = true;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
