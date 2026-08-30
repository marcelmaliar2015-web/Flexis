using Flexis.Application.Users;
using Flexis.Domain.Users;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Flexis.Infrastructure.Security;

public static class UserSeeder
{
    public static async Task SeedIfEmptyAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<FlexisDbContext>();
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var seed = services.GetRequiredService<IOptions<AuthSeedSettings>>().Value;
        var hasher = services.GetRequiredService<IUserPasswordHasher>();

        db.Users.Add(User.Create(
            seed.AdminEmail.Trim().ToLowerInvariant(),
            "Flexis Admin",
            UserRole.Admin,
            hasher.Hash(seed.AdminPassword)));
        db.Users.Add(User.Create(
            seed.UserEmail.Trim().ToLowerInvariant(),
            "Flexis User",
            UserRole.User,
            hasher.Hash(seed.UserPassword)));
        db.Users.Add(User.Create(
            seed.ViewerEmail.Trim().ToLowerInvariant(),
            "Flexis Viewer",
            UserRole.Viewer,
            hasher.Hash(seed.ViewerPassword)));

        await db.SaveChangesAsync(cancellationToken);
    }
}
