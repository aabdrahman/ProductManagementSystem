using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagementSystem.Api.Entities.Models;

namespace ProductManagementSystem.Api.Data.EntityConfiguration;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UserId);

        builder.HasIndex(x => x.IsActive);

        builder.HasIndex(x => x.NormalizedName)
            .IsUnique();

        builder.HasQueryFilter(x => x.IsActive);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.NormalizedName)
            .IsRequired()
            .HasComputedColumnSql("UPPER([Name])")
            .HasMaxLength(100);

        builder.HasMany(x => x.Users)
            .WithOne(x => x.AssignedRole)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
