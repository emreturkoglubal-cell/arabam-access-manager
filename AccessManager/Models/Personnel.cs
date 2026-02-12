namespace AccessManager.Models;

public class Personnel
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public string? Position { get; set; }
    public Guid? ManagerId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public PersonnelStatus Status { get; set; }
    public Guid? RoleId { get; set; }
    public string? Location { get; set; }

    // Navigation
    public Department? Department { get; set; }
    public Personnel? Manager { get; set; }
    public Role? Role { get; set; }
}
