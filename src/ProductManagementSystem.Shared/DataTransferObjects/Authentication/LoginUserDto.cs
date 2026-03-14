using System.ComponentModel.DataAnnotations;

namespace ProductManagementSystem.Shared.DataTransferObjects.Authentication;

public record class LoginUserDto
{
    [Required(ErrorMessage = "Email Address is required")]
    [EmailAddress(ErrorMessage = "Kindly provide a valid email")]
    public string Email { get; set; }
    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; }
}
