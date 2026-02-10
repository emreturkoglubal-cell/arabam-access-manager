namespace AccessManager.Domain.Constants;

/// <summary>
/// Uygulama rol adları; [Authorize(Roles = "...")] ve menü yetkilerinde kullanılır.
/// AppRole enum ToString() ile eşleşmelidir.
/// </summary>
public static class AppRoleNames
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string User = "User";
    public const string Auditor = "Auditor";
    public const string Viewer = "Viewer";
}
