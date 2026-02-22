using AccessManager.Domain.Enums;

namespace AccessManager.Domain.Entities;

/// <summary>
/// Kaynak sistem (erişim verilebilecek uygulama, altyapı veya lisans). Ad, kod, tür (SystemType), kritiklik (CriticalLevel) ve sistem sahibi (OwnerId) bilgilerini tutar.
/// Erişim talepleri ve personel erişimleri bu sistem üzerinden tanımlanır.
/// </summary>
public class ResourceSystem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public SystemType SystemType { get; set; }
    public CriticalLevel CriticalLevel { get; set; }
    public int? OwnerId { get; set; }
    public string? Description { get; set; }

    public Personnel? Owner { get; set; }
}
