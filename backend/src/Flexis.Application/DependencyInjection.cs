using Flexis.Application.Auth;
using Flexis.Application.Google;
using Flexis.Application.JobApplication;
using Flexis.Application.MailCheck;
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
        services.AddScoped<GoogleAccessTokenService>();
        services.AddScoped<JobCatalogService>();
        services.AddScoped<JobPipelineService>();
        services.AddScoped<JobFinancialService>();
        services.AddScoped<JobResumeService>();
        services.AddScoped<JobApplicationLogService>();
        services.AddScoped<JobApplicationActivity>();
        services.AddScoped<GoogleDriveLayoutService>();
        services.AddScoped<MailCheckService>();
        services.AddScoped<MailConnectionService>();
        services.AddScoped<MailAccessTokenService>();
        return services;
    }
}
