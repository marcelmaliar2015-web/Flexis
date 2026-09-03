using Flexis.Domain.JobApplication;
using Flexis.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobFinancialSnapshotConfiguration : IEntityTypeConfiguration<JobFinancialSnapshot>
{
    public void Configure(EntityTypeBuilder<JobFinancialSnapshot> builder)
    {
        builder.ToTable("job_financial_snapshots");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.TodayPrice).HasPrecision(12, 2);
        builder.Property(item => item.ArchivedPrice).HasPrecision(12, 2);
        builder.Property(item => item.LifetimePrice).HasPrecision(12, 2);
        builder.HasIndex(item => new { item.UserId, item.CapturedHour }).IsUnique();
        builder.HasIndex(item => new { item.UserId, item.CapturedOn });
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
