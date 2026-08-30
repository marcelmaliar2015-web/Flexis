using Flexis.Domain.JobApplication;
using Flexis.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobPipelineEntryConfiguration : IEntityTypeConfiguration<JobPipelineEntry>
{
    public void Configure(EntityTypeBuilder<JobPipelineEntry> builder)
    {
        builder.ToTable("job_pipeline_entries");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.LocationName).HasMaxLength(100).IsRequired();
        builder.HasIndex(entry => new { entry.UserId, entry.ProfileId, entry.SourceId, entry.LocationSheetId })
            .IsUnique();
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entry => entry.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
