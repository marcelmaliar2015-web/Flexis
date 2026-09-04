using Flexis.Domain.Google;
using Flexis.Domain.JobApplication;
using Flexis.Domain.MailCheck;
using Flexis.Domain.Microsoft;
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

    public DbSet<MicrosoftClientCredentials> MicrosoftClientCredentials => Set<MicrosoftClientCredentials>();

    public DbSet<JobCatalogItem> JobCatalogItems => Set<JobCatalogItem>();

    public DbSet<JobPipelineEntry> JobPipelineEntries => Set<JobPipelineEntry>();

    public DbSet<JobProfileBannedCompany> JobProfileBannedCompanies => Set<JobProfileBannedCompany>();

    public DbSet<JobFinancialSettings> JobFinancialSettings => Set<JobFinancialSettings>();

    public DbSet<JobFinancialSnapshot> JobFinancialSnapshots => Set<JobFinancialSnapshot>();

    public DbSet<JobResumeSettings> JobResumeSettings => Set<JobResumeSettings>();

    public DbSet<JobProfileResumeSettings> JobProfileResumeSettings => Set<JobProfileResumeSettings>();

    public DbSet<JobApplicationLog> JobApplicationLogs => Set<JobApplicationLog>();

    public DbSet<MailCheckSettings> MailCheckSettings => Set<MailCheckSettings>();

    public DbSet<MailCheckProcessedMessage> MailCheckProcessedMessages => Set<MailCheckProcessedMessage>();

    public DbSet<MailCheckActionLog> MailCheckActionLogs => Set<MailCheckActionLog>();

    public DbSet<MailCheckUsageHour> MailCheckUsageHours => Set<MailCheckUsageHour>();

    public DbSet<MailConnection> MailConnections => Set<MailConnection>();

    public DbSet<MailCheckScanState> MailCheckScanStates => Set<MailCheckScanState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlexisDbContext).Assembly);
    }
}
