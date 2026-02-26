using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagementSystem.Api.Entities.Models;

namespace ProductManagementSystem.Api.Data.EntityConfiguration;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Rating);

        builder.ToTable(table => table.HasCheckConstraint("CK_Review_Rating", "[Rating] BETWEEN 1 AND 5"));

        builder.HasIndex(x => x.ReviewerName);

        builder.Property(x => x.ReviewerName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.ProductName)
            .IsRequired(false)
            .HasMaxLength(100);

        builder.Property(x => x.ReviewText)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.Rating)
            .IsRequired();
    }
}
