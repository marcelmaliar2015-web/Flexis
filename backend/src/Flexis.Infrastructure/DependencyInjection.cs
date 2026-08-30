using Flexis.Infrastructure.Persistence.Mongo;
using Flexis.Infrastructure.Persistence.Postgres;
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

        services.AddHealthChecks()
            .AddDbContextCheck<FlexisDbContext>("postgres")
            .AddCheck<MongoHealthCheck>("mongo", timeout: TimeSpan.FromSeconds(3));

        return services;
    }
}
