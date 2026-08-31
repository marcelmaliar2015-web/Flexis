using Flexis.Domain.JobApplication;
using Flexis.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobApplicationLogConfiguration : IEntityTypeConfiguration<JobApplicationLog>
{
    public void Configure(EntityTypeBuilder<JobApplicationLog> builder)
    {
        builder.ToTable("job_application_logs");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Category).HasMaxLength(32).IsRequired();
        builder.Property(item => item.Action).HasMaxLength(64).IsRequired();
        builder.Property(item => item.Summary).HasMaxLength(240).IsRequired();
        builder.Property(item => item.Detail).HasMaxLength(2000).IsRequired();
        builder.HasIndex(item => new { item.UserId, item.OccurredAt });
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
