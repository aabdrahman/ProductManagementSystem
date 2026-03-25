using System.ComponentModel.DataAnnotations.Schema;

namespace ProductManagementSystem.Api.Entities.Models;

public class UserOtpVerification
{
    public Guid Id { get; set; }

    public string UserEmail { get; set; }
    public string GeneratedOTP { get; set; }
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(UserEmail))]
    public User UserToConfirmDetails { get; set; }
}
