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

        builder.HasIndex(x => x.IsActive);

        builder.HasIndex(x => x.OrderStatus);

        builder.HasIndex(x => x.OrderTrackingId);

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
            .IsRequired().HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.IsConfirmed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.DeliveryDate)
            .IsRequired(false);

        //builder.Property(x => x.OrderCount)
        //    .IsRequired();

        //builder.HasOne(x => x.OrderedProduct)
        //    .WithMany(x => x.Orders)
        //    .HasForeignKey(x => x.ProductId)
        //    .OnDelete(DeleteBehavior.ClientCascade);

        builder.Property(x => x.DeliveryAddress)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasQueryFilter(x => x.IsActive);

        builder.Property(x => x.OrderTrackingId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasMany(x => x.OrderLineItems)
            .WithOne(x => x.order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}
