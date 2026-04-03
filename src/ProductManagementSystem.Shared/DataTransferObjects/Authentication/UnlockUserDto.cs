using System.ComponentModel.DataAnnotations;

namespace ProductManagementSystem.Shared.DataTransferObjects.Authentication;

public record class UnlockUserDto
{
    [Required(ErrorMessage = "Email Address is required")]
    [EmailAddress(ErrorMessage = "Kindly provide a valid email")]
    public string Email { get; set; }

    [Required(ErrorMessage = "User Id is required.")]
    [Range(1, double.MaxValue, ErrorMessage = "Kindly provide a valid user id.")]
    public int UserId { get; set; }
    [Required(ErrorMessage = "OTP is a required field.")]
    [StringLength(6, ErrorMessage = "Enter a valid OTP")]
    public string OTP { get; set; }
}