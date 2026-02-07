using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagementSystem.Api.Entities.Models;

namespace ProductManagementSystem.Api.Data.EntityConfiguration;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Id);

        builder.HasIndex(x => x.ProductId);

        builder.Property(x => x.Ordertatus)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.DeliveryDate)
            .IsRequired(false);

        builder.Property(x => x.OrderCount)
            .IsRequired();

        builder.HasOne(x => x.OrderedProduct)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.ClientCascade);

    }
}
