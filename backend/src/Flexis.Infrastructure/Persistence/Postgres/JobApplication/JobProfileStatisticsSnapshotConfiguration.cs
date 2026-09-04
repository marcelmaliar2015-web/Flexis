using Flexis.Domain.JobApplication;
using Flexis.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobProfileStatisticsSnapshotConfiguration
    : IEntityTypeConfiguration<JobProfileStatisticsSnapshot>
{
    public void Configure(EntityTypeBuilder<JobProfileStatisticsSnapshot> builder)
    {
        builder.ToTable("job_profile_statistics_snapshots");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ProfileTitle).HasMaxLength(256).IsRequired();
        builder.Property(item => item.Price).HasPrecision(12, 2);
        builder.HasIndex(item => new { item.UserId, item.ProfileId, item.CapturedHour }).IsUnique();
        builder.HasIndex(item => new { item.UserId, item.CapturedHour });
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<JobCatalogItem>()
            .WithMany()
            .HasForeignKey(item => item.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
