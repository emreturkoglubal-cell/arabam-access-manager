using AccessManager.Application.Interfaces;
using AccessManager.Infrastructure.Data;
using AccessManager.Infrastructure.Services;
using AccessManager.UI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AccessManager.UI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAccessManagerServices(this IServiceCollection services)
    {
        services.AddSingleton<MockDataStore>();

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
