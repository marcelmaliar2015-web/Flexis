using Flexis.Domain.MailCheck;
using Flexis.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flexis.Infrastructure.Persistence.Postgres.MailCheck;

internal sealed class MailCheckActionLogConfiguration : IEntityTypeConfiguration<MailCheckActionLog>
{
    public void Configure(EntityTypeBuilder<MailCheckActionLog> builder)
    {
        builder.ToTable("mail_check_action_logs");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Source).HasMaxLength(16).IsRequired();
        builder.Property(item => item.MailboxEmail).HasMaxLength(320).IsRequired();
        builder.Property(item => item.MailboxProvider).HasMaxLength(32).IsRequired();
        builder.Property(item => item.MessageId).HasMaxLength(512).IsRequired();
        builder.Property(item => item.Subject).HasMaxLength(500).IsRequired();
        builder.Property(item => item.FromAddress).HasMaxLength(500).IsRequired();
        builder.Property(item => item.Action).HasMaxLength(64).IsRequired();
        builder.Property(item => item.Label).HasMaxLength(64).IsRequired();
        builder.Property(item => item.Detail).HasMaxLength(2000).IsRequired();
        builder.HasIndex(item => new { item.UserId, item.OccurredAt });
        builder.HasIndex(item => new { item.UserId, item.Source, item.OccurredAt });
        builder.HasIndex(item => new { item.UserId, item.Action, item.OccurredAt });
        builder.HasIndex(item => new { item.UserId, item.MailConnectionId, item.OccurredAt });
        builder.HasIndex(item => item.RunId);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MailConnection>()
            .WithMany()
            .HasForeignKey(item => item.MailConnectionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
