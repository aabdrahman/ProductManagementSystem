using System.ComponentModel.DataAnnotations;

namespace ProductManagementSystem.Shared.DataTransferObjects.Authentication;

public record class ValidateOtpRequestDto
{
    [Required(ErrorMessage = "Email Address is a required field.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string UserEmailAddress { get; set; }
    [Required(ErrorMessage = "OTP is a required field.")]
    [StringLength(6, ErrorMessage = "Enter a valid OTP")]
    public string OTP { get; set; }
}