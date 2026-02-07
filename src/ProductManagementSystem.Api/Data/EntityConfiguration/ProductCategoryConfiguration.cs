using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagementSystem.Api.Entities.Models;

namespace ProductManagementSystem.Api.Data.EntityConfiguration;

public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.HasIndex(x => x.Id);

        builder.Property(x => x.Id).IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.NormalizedName)
            .IsRequired()
            .HasMaxLength(100)
            .HasComputedColumnSql("UPPER([Name])");

        builder.HasMany(x => x.Products)
            .WithOne(x => x.productCategory)
            .HasForeignKey(x => x.ProductCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
