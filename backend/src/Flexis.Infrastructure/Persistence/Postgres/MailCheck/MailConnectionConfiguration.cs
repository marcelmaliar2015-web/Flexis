using Flexis.Domain.MailCheck;
using Flexis.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flexis.Infrastructure.Persistence.Postgres.MailCheck;

internal sealed class MailConnectionConfiguration : IEntityTypeConfiguration<MailConnection>
{
    public void Configure(EntityTypeBuilder<MailConnection> builder)
    {
        builder.ToTable("mail_connections");
        builder.HasKey(connection => connection.Id);
        builder.Property(connection => connection.Provider).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(connection => connection.ExternalSubject).HasMaxLength(256).IsRequired();
        builder.Property(connection => connection.Email).HasMaxLength(256).IsRequired();
        builder.Property(connection => connection.RefreshTokenProtected).IsRequired();
        builder.Property(connection => connection.AccessTokenProtected).IsRequired();
        builder.Property(connection => connection.GrantedScopes).HasMaxLength(1000).IsRequired();
        builder.HasIndex(connection => connection.UserId).IsUnique();
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(connection => connection.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
