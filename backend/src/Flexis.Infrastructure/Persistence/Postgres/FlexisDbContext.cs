using Flexis.Domain.Google;
using Flexis.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Flexis.Infrastructure.Persistence.Postgres;

public sealed class FlexisDbContext : DbContext
{
    public FlexisDbContext(DbContextOptions<FlexisDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<GoogleConnection> GoogleConnections => Set<GoogleConnection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlexisDbContext).Assembly);
    }
}
