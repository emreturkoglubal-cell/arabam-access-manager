using AccessManager.Domain.Enums;

namespace AccessManager.Domain.Entities;

public class Personnel
{
    public Guid Id { get; set; }
    public string SicilNo { get; set; } = string.Empty;
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
    /// <summary>Profil fotoğrafı URL'si; boşsa varsayılan avatar gösterilir.</summary>
    public string? ImageUrl { get; set; }
    /// <summary>10 üzerinden puan; güncellenince tek değer olarak kalır (birikmez).</summary>
    public decimal? Rating { get; set; }
    /// <summary>Yönetici yorumu.</summary>
    public string? ManagerComment { get; set; }

    public Department? Department { get; set; }
    public Personnel? Manager { get; set; }
    public Role? Role { get; set; }
}
