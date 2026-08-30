using Flexis.Domain.Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flexis.Infrastructure.Persistence.Postgres.Google;

internal sealed class GoogleClientCredentialsConfiguration : IEntityTypeConfiguration<GoogleClientCredentials>
{
    public void Configure(EntityTypeBuilder<GoogleClientCredentials> builder)
    {
        builder.ToTable("google_client_credentials");
        builder.HasKey(credentials => credentials.Id);
        builder.Property(credentials => credentials.ClientId).HasMaxLength(256).IsRequired();
        builder.Property(credentials => credentials.ClientSecretProtected).IsRequired();
    }
}
