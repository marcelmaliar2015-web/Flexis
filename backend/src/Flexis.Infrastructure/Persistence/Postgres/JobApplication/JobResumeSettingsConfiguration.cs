using Flexis.Domain.JobApplication;
using Flexis.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobResumeSettingsConfiguration : IEntityTypeConfiguration<JobResumeSettings>
{
    public void Configure(EntityTypeBuilder<JobResumeSettings> builder)
    {
        builder.ToTable("job_resume_settings");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.OwnerOptionsJson).IsRequired();
        builder.Property(item => item.JobMasterSpreadsheetId).HasMaxLength(128);
        builder.Property(item => item.JobMasterUrl).HasMaxLength(2048);
        builder.HasIndex(item => item.UserId).IsUnique();
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
