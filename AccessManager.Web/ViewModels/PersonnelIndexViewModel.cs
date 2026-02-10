using AccessManager.Domain.Entities;

namespace AccessManager.UI.ViewModels;

public class PersonnelIndexViewModel
{
    public IReadOnlyList<Personnel> PersonnelList { get; set; } = new List<Personnel>();
    public IReadOnlyList<Department> Departments { get; set; } = new List<Department>();
    public IReadOnlyList<Role> Roles { get; set; } = new List<Role>();
    public Dictionary<Guid, string> ManagerNames { get; set; } = new();
    public Guid? FilterDepartmentId { get; set; }
    public bool? FilterActiveOnly { get; set; } = true;
}
