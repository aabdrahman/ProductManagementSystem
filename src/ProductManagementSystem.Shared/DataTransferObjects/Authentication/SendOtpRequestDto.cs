using System.ComponentModel.DataAnnotations;

namespace ProductManagementSystem.Shared.DataTransferObjects.Authentication;

public record class SendOtpRequestDto
{
    [Required(ErrorMessage = "Email Address is a required field.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string UserEmailAddress { get; set; }
    [Required(ErrorMessage = "User Id is required.")]
    [Range(1, double.MaxValue, ErrorMessage = "Kindly provide a valid user id.")]
    public int UserId { get; set; }
}
