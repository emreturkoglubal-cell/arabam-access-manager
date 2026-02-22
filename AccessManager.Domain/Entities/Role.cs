namespace AccessManager.Domain.Entities;

/// <summary>
/// İş rolü (Görev). Personelin atandığı rol; rolün sistem bazlı yetkileri RolePermission ile tanımlanır.
/// Uygulama giriş yetkisi (AppRole) ile karıştırılmamalı; bu domain/iş rolüdür.
/// </summary>
public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Code { get; set; }
}
