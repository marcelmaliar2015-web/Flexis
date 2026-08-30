using Flexis.Application.Auth;
using Flexis.Application.Google;
using Flexis.Application.JobApplication;
using Flexis.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Flexis.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<UserManagementService>();
        services.AddScoped<GoogleConnectionService>();
        services.AddScoped<JobCatalogService>();
        return services;
    }
}
