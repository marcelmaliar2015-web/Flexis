using Flexis.Domain.MailCheck;
using Flexis.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flexis.Infrastructure.Persistence.Postgres.MailCheck;

internal sealed class MailCheckSettingsConfiguration : IEntityTypeConfiguration<MailCheckSettings>
{
    public void Configure(EntityTypeBuilder<MailCheckSettings> builder)
    {
        builder.ToTable("mail_check_settings");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ApiKeyProtected).HasColumnType("text");
        builder.Property(item => item.Model).HasMaxLength(128).IsRequired();
        builder.Property(item => item.LastError).HasMaxLength(500).IsRequired();
        builder.HasIndex(item => item.UserId).IsUnique();
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
