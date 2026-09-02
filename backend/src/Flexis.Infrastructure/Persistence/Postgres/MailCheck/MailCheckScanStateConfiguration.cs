using Flexis.Domain.MailCheck;
using Flexis.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flexis.Infrastructure.Persistence.Postgres.MailCheck;

internal sealed class MailCheckScanStateConfiguration : IEntityTypeConfiguration<MailCheckScanState>
{
    public void Configure(EntityTypeBuilder<MailCheckScanState> builder)
    {
        builder.ToTable("mail_check_scan_states");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => item.MailConnectionId).IsUnique();
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MailConnection>()
            .WithMany()
            .HasForeignKey(item => item.MailConnectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
