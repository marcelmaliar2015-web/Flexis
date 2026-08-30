using Flexis.Application.JobApplication;
using Flexis.Domain.JobApplication;
using Flexis.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobCatalogItemConfiguration : IEntityTypeConfiguration<JobCatalogItem>
{
    public void Configure(EntityTypeBuilder<JobCatalogItem> builder)
    {
        builder.ToTable("job_catalog_items");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Title).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Url).HasMaxLength(2048).IsRequired();
        builder.Property(item => item.Kind).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(item => new { item.UserId, item.Kind, item.Title }).IsUnique();
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
