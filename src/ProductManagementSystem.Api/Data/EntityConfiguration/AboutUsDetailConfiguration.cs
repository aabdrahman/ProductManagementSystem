using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagementSystem.Api.Entities.Models;

namespace ProductManagementSystem.Api.Data.EntityConfiguration;

public class AboutUsDetailConfiguration : IEntityTypeConfiguration<AboutUsDetail>
{
    public void Configure(EntityTypeBuilder<AboutUsDetail> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Id);

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(x => x.Title)
            .HasMaxLength(50).IsRequired();

        builder.Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.DeletedBy)
            .IsRequired(false)
            .HasMaxLength(100);
    }
}