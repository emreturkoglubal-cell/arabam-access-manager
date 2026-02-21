using AccessManager.UI.Extensions;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Lokal overrides (commit edilmez). Canlıda: appsettings.Production.json + Cloud Run env değişkenleri kullanılır.
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

builder.Services.AddControllersWithViews(options =>
{
    var policy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy));
});

builder.Services.AddAccessManagerAuthentication();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAccessManagerServices();

var app = builder.Build();

// Extended log: Error/Critical loglar extended_logs tablosuna (IP, URL, user agent vb.) yazılır
if (app.Services.GetService<ILoggerFactory>() is Microsoft.Extensions.Logging.LoggerFactory loggerFactory)
{
    loggerFactory.AddProvider(app.Services.GetRequiredService<AccessManager.UI.Logging.ExtendedLogLoggerProvider>());
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Development modunda static files için cache'i devre dışı bırak (hot reload için)
if (app.Environment.IsDevelopment())
{
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            // CSS, JS ve diğer static dosyalar için cache'i devre dışı bırak
            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            ctx.Context.Response.Headers.Append("Pragma", "no-cache");
            ctx.Context.Response.Headers.Append("Expires", "0");
        }
    });
}
else
{
    app.UseStaticFiles();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Git RepoPath doğrulama: canlıda path ve .git var mı logla (doğru path'ı anlamak için)
var repoPath = app.Configuration["Git:RepoPath"]?.Trim();
if (!string.IsNullOrEmpty(repoPath))
{
    var fullPath = Path.GetFullPath(repoPath);
    var exists = Directory.Exists(fullPath);
    var hasGit = exists && Directory.Exists(Path.Combine(fullPath, ".git"));
    app.Logger.LogInformation(
        "Git:RepoPath = {RepoPath} (resolved: {FullPath}), Exists = {Exists}, HasGit = {HasGit}. AI push çalışması için HasGit true olmalı.",
        repoPath, fullPath, exists, hasGit);
}
else
{
    app.Logger.LogInformation("Git:RepoPath boş. AI sadece soru-cevap yapabilir, commit/push yapmaz.");
}

app.Run();
