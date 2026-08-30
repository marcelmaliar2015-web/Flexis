using Flexis.Infrastructure.Persistence.Postgres;
using Flexis.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flexis.Infrastructure;

public static class PostgresStartup
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        bool seedUsers,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FlexisDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        if (seedUsers)
        {
            await UserSeeder.SeedIfEmptyAsync(scope.ServiceProvider, cancellationToken);
        }
    }
}
