using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagementSystem.Api.Entities.Models;

namespace ProductManagementSystem.Api.Data.EntityConfiguration;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Id);

        builder.HasIndex(x => x.ProductCategoryId);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.NormalizedName)
            .IsRequired()
            .HasMaxLength(100)
            .HasComputedColumnSql("UPPER([Name])");

        builder.Property(x => x.Description)
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.Property(x => x.CostPrice)
            .IsRequired()
            .HasColumnType("decimal(10, 2)");

        builder.Property(x => x.SellingPrice)
            .IsRequired()
            .HasColumnType("decimal(10, 2)");

        builder.Property(x => x.CurrentCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.ProductCategoryId)
            .IsRequired();

        builder.HasOne(x => x.productCategory)
            .WithMany()
            .HasForeignKey(x => x.ProductCategoryId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(x => x.Orders)
            .WithOne(x => x.OrderedProduct)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
