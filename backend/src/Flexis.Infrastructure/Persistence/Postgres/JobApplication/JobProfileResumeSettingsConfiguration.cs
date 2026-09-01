using Flexis.Domain.JobApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobProfileResumeSettingsConfiguration : IEntityTypeConfiguration<JobProfileResumeSettings>
{
    public void Configure(EntityTypeBuilder<JobProfileResumeSettings> builder)
    {
        builder.ToTable("job_profile_resume_settings");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Prompt).IsRequired();
        builder.Property(item => item.Owner).IsRequired().HasMaxLength(120);
        builder.HasIndex(item => item.ProfileId).IsUnique();
        builder.HasOne<JobCatalogItem>()
            .WithMany()
            .HasForeignKey(item => item.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
