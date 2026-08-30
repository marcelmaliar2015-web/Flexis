using Flexis.Application.Auth;
using Flexis.Application.Google;
using Flexis.Application.Users;
using Flexis.Infrastructure.Google;
using Flexis.Infrastructure.Persistence.Mongo;
using Flexis.Infrastructure.Persistence.Postgres;
using Flexis.Infrastructure.Persistence.Postgres.Google;
using Flexis.Infrastructure.Persistence.Postgres.Users;
using Flexis.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Flexis.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddDbContext<FlexisDbContext>(options =>
            options.UseNpgsql(postgresConnectionString));

        services.AddOptions<MongoSettings>()
            .Bind(configuration.GetSection(MongoSettings.SectionName))
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.ConnectionString), "Mongo:ConnectionString is required.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Database), "Mongo:Database is required.")
            .ValidateOnStart();

        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Issuer), "Jwt:Issuer is required.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Audience), "Jwt:Audience is required.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.SigningKey) && settings.SigningKey.Length >= 32, "Jwt:SigningKey must be at least 32 characters.")
            .Validate(settings => settings.AccessTokenMinutes > 0, "Jwt:AccessTokenMinutes must be positive.")
            .ValidateOnStart();

        services.AddOptions<AuthSeedSettings>()
            .Bind(configuration.GetSection(AuthSeedSettings.SectionName));

        services.AddOptions<GoogleOAuthSettings>()
            .Bind(configuration.GetSection(GoogleOAuthSettings.SectionName))
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.TokenProtectionKey) && settings.TokenProtectionKey.Length >= 32,
                "Google:TokenProtectionKey must be at least 32 characters.")
            .ValidateOnStart();

        services.AddOptions<FrontendOriginSettings>()
            .Bind(configuration.GetSection(FrontendOriginSettings.SectionName))
            .Validate(settings => settings.Origins is { Length: > 0 }, "Frontend:Origins is required.")
            .ValidateOnStart();

        services.AddMemoryCache();
        services.AddHttpClient<IGoogleOAuthGateway, GoogleOAuthClient>();
        services.AddSingleton<IGoogleTokenProtector, AesGoogleTokenProtector>();
        services.AddSingleton<IGoogleOAuthStateStore, MemoryGoogleOAuthStateStore>();
        services.AddSingleton<IFrontendOrigins, ConfigurationFrontendOrigins>();
        services.AddScoped<IGoogleConnectionRepository, GoogleConnectionRepository>();

        services.AddSingleton<IMongoClient>(serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<MongoSettings>>().Value;
            var clientSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
            clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
            clientSettings.ConnectTimeout = TimeSpan.FromSeconds(3);
            return new MongoClient(clientSettings);
        });

        services.AddSingleton(serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<MongoSettings>>().Value;
            var client = serviceProvider.GetRequiredService<IMongoClient>();
            return client.GetDatabase(settings.Database);
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IUserPasswordHasher, AspNetUserPasswordHasher>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

        services.AddHealthChecks()
            .AddDbContextCheck<FlexisDbContext>("postgres")
            .AddCheck<MongoHealthCheck>("mongo", timeout: TimeSpan.FromSeconds(3));

        return services;
    }
}
