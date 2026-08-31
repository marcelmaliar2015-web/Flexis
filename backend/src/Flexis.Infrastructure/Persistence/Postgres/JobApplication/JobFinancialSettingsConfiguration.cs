using Flexis.Domain.JobApplication;
using Flexis.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobFinancialSettingsConfiguration : IEntityTypeConfiguration<JobFinancialSettings>
{
    public void Configure(EntityTypeBuilder<JobFinancialSettings> builder)
    {
        builder.ToTable("job_financial_settings");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ApplyRate).HasPrecision(12, 4);
        builder.Property(item => item.BonusRate).HasPrecision(12, 4);
        builder.HasIndex(item => item.UserId).IsUnique();
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
