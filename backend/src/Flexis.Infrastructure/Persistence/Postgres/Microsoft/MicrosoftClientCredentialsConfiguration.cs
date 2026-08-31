using Flexis.Domain.Microsoft;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flexis.Infrastructure.Persistence.Postgres.Microsoft;

internal sealed class MicrosoftClientCredentialsConfiguration : IEntityTypeConfiguration<MicrosoftClientCredentials>
{
    public void Configure(EntityTypeBuilder<MicrosoftClientCredentials> builder)
    {
        builder.ToTable("microsoft_client_credentials");
        builder.HasKey(credentials => credentials.Id);
        builder.Property(credentials => credentials.ClientId).HasMaxLength(256).IsRequired();
        builder.Property(credentials => credentials.ClientSecretProtected).IsRequired();
    }
}
