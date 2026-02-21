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

// Git/CodeContext path doğrulama: canlıda read_file ve push için repo yolunun doğru olduğunu kontrol et
var repoPath = app.Configuration["Git:RepoPath"]?.Trim();
var codeContextBase = app.Configuration["CodeContext:BasePath"]?.Trim();
if (!string.IsNullOrEmpty(repoPath))
{
    var fullPath = Path.GetFullPath(repoPath);
    var exists = Directory.Exists(fullPath);
    var hasGit = exists && Directory.Exists(Path.Combine(fullPath, ".git"));
    var sampleFile = Path.Combine(fullPath, "AccessManager.Domain", "AccessManager.Domain.csproj");
    var hasSource = exists && File.Exists(sampleFile);
    app.Logger.LogInformation(
        "Git:RepoPath = {RepoPath} (resolved: {FullPath}), Exists = {Exists}, HasGit = {HasGit}, HasSource = {HasSource}. read_file için HasSource true olmalı.",
        repoPath, fullPath, exists, hasGit, hasSource);
    if (exists && !hasSource)
        app.Logger.LogWarning("Git:RepoPath altında kaynak kod yok (AccessManager.Domain bulunamadı). Canlıda read_file çalışmaz; Git:RepoPath ve Dockerfile /app/repo kopyası kontrol edin.");
}
else
{
    app.Logger.LogInformation("Git:RepoPath boş. AI sadece soru-cevap yapabilir; read_file ve commit/push çalışmaz.");
}
if (!string.IsNullOrEmpty(codeContextBase))
    app.Logger.LogInformation("CodeContext:BasePath = {BasePath}. Proje yapısı listesi bu dizinden alınır; read_file ile aynı repo olmalı.", codeContextBase);

app.Run();
