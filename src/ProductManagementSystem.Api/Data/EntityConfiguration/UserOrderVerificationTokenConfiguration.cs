using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagementSystem.Api.Entities.Models;

namespace ProductManagementSystem.Api.Data.EntityConfiguration;

public class UserOrderVerificationTokenConfiguration : IEntityTypeConfiguration<UserOrderVerificationToken>
{
    public void Configure(EntityTypeBuilder<UserOrderVerificationToken> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Id);

        builder.HasIndex(x => x.OrderId);

        builder.HasIndex(x => x.VerificationToken).IsUnique();


        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()").IsRequired();

        builder.HasOne(x => x.OrderToConfirm)
            .WithMany(x => x.UserOrderVerificationTokens)
            .HasForeignKey(x => x.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
