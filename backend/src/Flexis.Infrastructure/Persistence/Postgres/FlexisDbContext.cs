using Flexis.Domain.Google;
using Flexis.Domain.JobApplication;
using Flexis.Domain.MailCheck;
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

    public DbSet<GoogleClientCredentials> GoogleClientCredentials => Set<GoogleClientCredentials>();

    public DbSet<JobCatalogItem> JobCatalogItems => Set<JobCatalogItem>();

    public DbSet<JobPipelineEntry> JobPipelineEntries => Set<JobPipelineEntry>();

    public DbSet<JobPipelineBannedCompany> JobPipelineBannedCompanies => Set<JobPipelineBannedCompany>();

    public DbSet<JobFinancialSettings> JobFinancialSettings => Set<JobFinancialSettings>();

    public DbSet<JobApplicationLog> JobApplicationLogs => Set<JobApplicationLog>();

    public DbSet<MailCheckSettings> MailCheckSettings => Set<MailCheckSettings>();

    public DbSet<MailCheckProcessedMessage> MailCheckProcessedMessages => Set<MailCheckProcessedMessage>();

    public DbSet<MailConnection> MailConnections => Set<MailConnection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlexisDbContext).Assembly);
    }
}
