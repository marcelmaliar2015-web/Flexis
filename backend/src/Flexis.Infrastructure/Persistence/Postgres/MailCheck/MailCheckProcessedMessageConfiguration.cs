using Flexis.Domain.MailCheck;
using Flexis.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flexis.Infrastructure.Persistence.Postgres.MailCheck;

internal sealed class MailCheckProcessedMessageConfiguration : IEntityTypeConfiguration<MailCheckProcessedMessage>
{
    public void Configure(EntityTypeBuilder<MailCheckProcessedMessage> builder)
    {
        builder.ToTable("mail_check_processed_messages");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.GmailMessageId).HasMaxLength(128).IsRequired();
        builder.Property(item => item.Decision).HasMaxLength(32).IsRequired();
        builder.HasIndex(item => new { item.UserId, item.GmailMessageId }).IsUnique();
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
