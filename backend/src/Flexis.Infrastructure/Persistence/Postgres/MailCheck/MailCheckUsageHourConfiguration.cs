using Flexis.Domain.MailCheck;
using Flexis.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flexis.Infrastructure.Persistence.Postgres.MailCheck;

internal sealed class MailCheckUsageHourConfiguration : IEntityTypeConfiguration<MailCheckUsageHour>
{
    public void Configure(EntityTypeBuilder<MailCheckUsageHour> builder)
    {
        builder.ToTable("mail_check_usage_hours");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.LastModel).HasMaxLength(128).IsRequired();
        builder.Property(item => item.EstimatedCostUsd).HasPrecision(18, 8);
        builder.HasIndex(item => new { item.UserId, item.CapturedHour }).IsUnique();
        builder.HasIndex(item => new { item.UserId, item.CapturedOn });
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
