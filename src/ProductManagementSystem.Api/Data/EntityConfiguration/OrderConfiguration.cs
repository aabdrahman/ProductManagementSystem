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

        builder.HasIndex(x => x.OrderStatus);

        builder.ToTable(table => table.HasCheckConstraint("CK_OrderStatus", "[OrderStatus] IN ('Pending', 'Processing', 'Delivered', 'Cancelled')"));

        builder.HasIndex(x => x.CreatedAt);

        builder.Property(x => x.OrderStatus)
            .IsRequired()
            .HasMaxLength(50)
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

        builder.HasQueryFilter(x => x.IsActive);

    }
}
