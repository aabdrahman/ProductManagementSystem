using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagementSystem.Api.Entities.Models;

namespace ProductManagementSystem.Api.Data.EntityConfiguration;

public class UserOtpVerificationConfiguration : IEntityTypeConfiguration<UserOtpVerification>
{
    public void Configure(EntityTypeBuilder<UserOtpVerification> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UserEmail);

        builder.HasIndex(x => x.CreatedAt);

        builder.Property(x => x.UserEmail)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.UserToConfirmDetails)
            .WithMany()
            .HasForeignKey(x => x.UserEmail).HasPrincipalKey(x => x.UserEmailAddress).IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

    }
}
