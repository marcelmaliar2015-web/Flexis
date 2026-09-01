using Flexis.Domain.JobApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobProfileBannedCompanyConfiguration : IEntityTypeConfiguration<JobProfileBannedCompany>
{
    public void Configure(EntityTypeBuilder<JobProfileBannedCompany> builder)
    {
        builder.ToTable("job_profile_banned_companies");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(item => item.MatchKey).HasMaxLength(200).IsRequired();
        builder.HasIndex(item => new { item.ProfileId, item.MatchKey }).IsUnique();
        builder.HasOne<JobCatalogItem>()
            .WithMany()
            .HasForeignKey(item => item.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
