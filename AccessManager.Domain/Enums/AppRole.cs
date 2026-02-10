namespace AccessManager.Domain.Enums;

/// <summary>
/// Uygulama seviyesi roller (giriş ve menü yetkisi için).
/// Domain Role (iş rolü) ile karıştırılmamalı.
/// </summary>
public enum AppRole
{
    Admin = 0,
    Manager = 1,
    User = 2,
    Auditor = 3,
    Viewer = 4
}
