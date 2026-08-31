using Flexis.Domain.JobApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobPipelineBannedCompanyConfiguration : IEntityTypeConfiguration<JobPipelineBannedCompany>
{
    public void Configure(EntityTypeBuilder<JobPipelineBannedCompany> builder)
    {
        builder.ToTable("job_pipeline_banned_companies");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(item => item.MatchKey).HasMaxLength(200).IsRequired();
        builder.HasIndex(item => new { item.PipelineEntryId, item.MatchKey }).IsUnique();
        builder.HasOne<JobPipelineEntry>()
            .WithMany()
            .HasForeignKey(item => item.PipelineEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
