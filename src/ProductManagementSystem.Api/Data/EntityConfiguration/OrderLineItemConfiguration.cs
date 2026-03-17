using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagementSystem.Api.Entities.Models;

namespace ProductManagementSystem.Api.Data.EntityConfiguration;

public class OrderLineItemConfiguration : IEntityTypeConfiguration<OrderLineItem>
{
    public void Configure(EntityTypeBuilder<OrderLineItem> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Id);

        builder.HasIndex(x => x.OrderId);

        builder.HasIndex(x => x.ProductId);

        builder.HasIndex(x => x.IsActive);

        builder.ToTable(table => table.HasCheckConstraint("CK_OrderLineItem_QuantityOrdered", "[QuantityOrdered] > 0"));

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.QuantityOrdered)
            .IsRequired();

        builder.HasQueryFilter(x => x.IsActive);

        builder.HasOne(x => x.order)
            .WithMany(x => x.OrderLineItems)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasOne(x => x.OrderedProduct)
            .WithMany(x => x.OrderLineItems)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}