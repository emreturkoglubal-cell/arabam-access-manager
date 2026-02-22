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

// Cloud Run: /app salt okunur; apply_diff ve push için repo yazılabilir bir yerde olmalı. Git:RepoPath=/tmp/repo ise /app/repo'yu oraya kopyala.
var repoPath = app.Configuration["Git:RepoPath"]?.Trim();
var repoFullPath = string.IsNullOrEmpty(repoPath) ? null : Path.GetFullPath(repoPath);
var isTmpRepo = !string.IsNullOrEmpty(repoFullPath) && repoFullPath.Replace('\\', '/').TrimEnd('/').EndsWith("tmp/repo", StringComparison.OrdinalIgnoreCase);
if (isTmpRepo && !Directory.Exists(repoFullPath) && Directory.Exists("/app/repo"))
{
    try
    {
        Directory.CreateDirectory(repoFullPath!);
        CopyDirectory("/app/repo", repoFullPath!);
        app.Logger.LogError("Cloud Run: /app/repo yazılabilir olması için /tmp/repo'ya kopyalandı. apply_diff ve push artık çalışabilir.");
        EnsureRepoOnCleanMain(repoFullPath!, app.Configuration, app.Logger);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Cloud Run: /app/repo -> /tmp/repo kopyalanamadı. apply_diff/push başarısız olabilir (dosya sistemi salt okunur).");
    }
}

static void EnsureRepoOnCleanMain(string repoPath, IConfiguration config, ILogger logger)
{
    try
    {
        var (ok, remoteUrlRaw) = RunGit(repoPath, "remote get-url origin");
        if (!ok || string.IsNullOrWhiteSpace(remoteUrlRaw)) return;
        var originUrl = remoteUrlRaw!.Split('\n')[0].Trim();
        var token = config["Git:Token"]?.Trim();
        var fetchTarget = !string.IsNullOrEmpty(token) ? InjectToken(originUrl, token) : "origin";
        RunGit(repoPath, "fetch \"" + fetchTarget + "\" main");
        var (coOk, _) = RunGit(repoPath, "checkout main");
        if (!coOk) RunGit(repoPath, "checkout -b main origin/main");
        RunGit(repoPath, "reset --hard origin/main");
        logger.LogError("Git: Repo main branch üzerinde ve origin/main ile sıfırlandı. Base branch her zaman main.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Git: EnsureRepoOnCleanMain atlandı.");
    }
}

static string InjectToken(string url, string token)
{
    if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return "https://" + token + "@" + url.Substring(8);
    if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) return "http://" + token + "@" + url.Substring(7);
    return url;
}

static (bool success, string output) RunGit(string repoPath, string arguments)
{
    try
    {
        using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = repoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        if (p == null) return (false, "");
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode == 0, (stdout + "\n" + stderr).Trim());
    }
    catch { return (false, ""); }
}

// Git/CodeContext path doğrulama: canlıda read_file ve push için repo yolunun doğru olduğunu kontrol et
var codeContextBase = app.Configuration["CodeContext:BasePath"]?.Trim();
if (!string.IsNullOrEmpty(repoPath))
{
    var fullPath = Path.GetFullPath(repoPath);
    var exists = Directory.Exists(fullPath);
    var hasGit = exists && Directory.Exists(Path.Combine(fullPath, ".git"));
    var sampleFile = Path.Combine(fullPath, "AccessManager.Domain", "AccessManager.Domain.csproj");
    var hasSource = exists && File.Exists(sampleFile);
    app.Logger.LogError(
        "Git:RepoPath = {RepoPath} (resolved: {FullPath}), Exists = {Exists}, HasGit = {HasGit}, HasSource = {HasSource}. read_file için HasSource true olmalı.",
        repoPath, fullPath, exists, hasGit, hasSource);
    if (exists && !hasSource)
        app.Logger.LogError("Git:RepoPath altında kaynak kod yok (AccessManager.Domain bulunamadı). Canlıda read_file çalışmaz; Git:RepoPath ve Dockerfile /app/repo kopyası kontrol edin.");
}
else
{
    app.Logger.LogError("Git:RepoPath boş. AI sadece soru-cevap yapabilir; read_file ve commit/push çalışmaz.");
}
if (!string.IsNullOrEmpty(codeContextBase))
    app.Logger.LogError("CodeContext:BasePath = {BasePath}. Proje yapısı listesi bu dizinden alınır; read_file ile aynı repo olmalı.", codeContextBase);

app.Run();

static void CopyDirectory(string sourceDir, string targetDir)
{
    foreach (var dir in Directory.GetDirectories(sourceDir))
    {
        var dest = Path.Combine(targetDir, Path.GetFileName(dir));
        Directory.CreateDirectory(dest);
        CopyDirectory(dir, dest);
    }
    foreach (var file in Directory.GetFiles(sourceDir))
    {
        File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), overwrite: true);
    }
}
