using Flexis.Domain.JobApplication;
using Flexis.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobListingCopyBatchConfiguration : IEntityTypeConfiguration<JobListingCopyBatch>
{
    public void Configure(EntityTypeBuilder<JobListingCopyBatch> builder)
    {
        builder.ToTable("job_listing_copy_batches");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.UserId, item.ProfileId, item.CopiedAt });
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<JobCatalogItem>()
            .WithMany()
            .HasForeignKey(item => item.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(item => item.Items)
            .WithOne()
            .HasForeignKey(item => item.BatchId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(item => item.Items).AutoInclude();
    }
}

internal sealed class JobListingCopyItemConfiguration : IEntityTypeConfiguration<JobListingCopyItem>
{
    public void Configure(EntityTypeBuilder<JobListingCopyItem> builder)
    {
        builder.ToTable("job_listing_copy_items");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ListingKey).HasMaxLength(1024);
        builder.HasIndex(item => new { item.BatchId, item.ListingKey }).IsUnique();
    }
}

internal sealed class JobListingStatusStateConfiguration : IEntityTypeConfiguration<JobListingStatusState>
{
    public void Configure(EntityTypeBuilder<JobListingStatusState> builder)
    {
        builder.ToTable("job_listing_status_states");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ListingKey).HasMaxLength(1024);
        builder.Property(item => item.Status).HasMaxLength(64);
        builder.HasIndex(item => new { item.UserId, item.ProfileId, item.ListingKey }).IsUnique();
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

internal sealed class JobListingStatusEventConfiguration : IEntityTypeConfiguration<JobListingStatusEvent>
{
    public void Configure(EntityTypeBuilder<JobListingStatusEvent> builder)
    {
        builder.ToTable("job_listing_status_events");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ListingKey).HasMaxLength(1024);
        builder.Property(item => item.Status).HasMaxLength(64);
        builder.HasIndex(item => new { item.UserId, item.OccurredAt });
        builder.HasIndex(item => new { item.UserId, item.ProfileId, item.OccurredAt });
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
