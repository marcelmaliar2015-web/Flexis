using Flexis.Domain.JobApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobListingProjectionConfiguration : IEntityTypeConfiguration<JobListingProjection>
{
    public void Configure(EntityTypeBuilder<JobListingProjection> builder)
    {
        builder.ToTable("job_listing_projections");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Source).HasMaxLength(32).IsRequired();
        builder.Property(item => item.ArchiveTab).HasMaxLength(100).IsRequired();
        builder.Property(item => item.ListingKey).HasMaxLength(1024).IsRequired();
        builder.Property(item => item.CompanyName).HasMaxLength(500).IsRequired();
        builder.Property(item => item.Position).HasMaxLength(500).IsRequired();
        builder.Property(item => item.Link).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.Jd).HasColumnType("text").IsRequired();
        builder.Property(item => item.Download).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.Status).HasMaxLength(64).IsRequired();
        builder.Property(item => item.Issue).HasMaxLength(1000).IsRequired();
        builder.Property(item => item.ProfileName).HasMaxLength(200).IsRequired();
        builder.Property(item => item.ContentHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(item => new { item.UserId, item.Source, item.ProfileId, item.ArchiveTab, item.ListingKey })
            .IsUnique();
        builder.HasIndex(item => new { item.UserId, item.ProfileId, item.Source });
        builder.HasOne<Domain.Users.User>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class JobSheetSyncStateConfiguration : IEntityTypeConfiguration<JobSheetSyncState>
{
    public void Configure(EntityTypeBuilder<JobSheetSyncState> builder)
    {
        builder.ToTable("job_sheet_sync_states");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.SpreadsheetId).HasMaxLength(128).IsRequired();
        builder.Property(item => item.DriveModifiedTime).HasMaxLength(64).IsRequired();
        builder.HasIndex(item => new { item.UserId, item.SpreadsheetId }).IsUnique();
        builder.HasOne<Domain.Users.User>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
