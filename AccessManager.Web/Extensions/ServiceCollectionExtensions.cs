using AccessManager.Application.Interfaces;
using AccessManager.Infrastructure.Repositories;
using AccessManager.Infrastructure.Services;
using AccessManager.UI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AccessManager.UI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAccessManagerServices(this IServiceCollection services)
    {
        // Connection string ile repository'leri kaydet (Dapper)
        services.AddScoped<IDepartmentRepository>(sp =>
        {
            var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
            return new DepartmentRepository(cs);
        });
        services.AddScoped<IRoleRepository>(sp =>
        {
            var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")!;
            return new RoleRepository(cs);
        });
        services.AddScoped<IPersonnelRepository>(sp =>
        {
            var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")!;
            return new PersonnelRepository(cs);
        });
        services.AddScoped<IResourceSystemRepository>(sp =>
        {
            var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")!;
            return new ResourceSystemRepository(cs);
        });
        services.AddScoped<IAppUserRepository>(sp =>
        {
            var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")!;
            return new AppUserRepository(cs);
        });
        services.AddScoped<IPersonnelAccessRepository>(sp =>
        {
            var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")!;
            return new PersonnelAccessRepository(cs);
        });
        services.AddScoped<IAccessRequestRepository>(sp =>
        {
            var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")!;
            return new AccessRequestRepository(cs);
        });
        services.AddScoped<IApprovalStepRepository>(sp =>
        {
            var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")!;
            return new ApprovalStepRepository(cs);
        });
        services.AddScoped<IAuditLogRepository>(sp =>
        {
            var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")!;
            return new AuditLogRepository(cs);
        });
        services.AddScoped<IAssetRepository>(sp =>
        {
            var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")!;
            return new AssetRepository(cs);
        });
        services.AddScoped<IAssetAssignmentRepository>(sp =>
        {
            var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")!;
            return new AssetAssignmentRepository(cs);
        });

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IPersonnelService, PersonnelService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<ISystemService, SystemService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPersonnelAccessService, PersonnelAccessService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAccessRequestService, AccessRequestService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddHttpClient();

        return services;
    }

    public static IServiceCollection AddAccessManagerAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            });

        return services;
    }
}
