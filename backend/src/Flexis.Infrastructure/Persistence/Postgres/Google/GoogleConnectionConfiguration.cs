using Flexis.Domain.Google;
using Flexis.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flexis.Infrastructure.Persistence.Postgres.Google;

internal sealed class GoogleConnectionConfiguration : IEntityTypeConfiguration<GoogleConnection>
{
    public void Configure(EntityTypeBuilder<GoogleConnection> builder)
    {
        builder.ToTable("google_connections");
        builder.HasKey(connection => connection.Id);
        builder.Property(connection => connection.GoogleSubject).HasMaxLength(256).IsRequired();
        builder.Property(connection => connection.GoogleEmail).HasMaxLength(256).IsRequired();
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
