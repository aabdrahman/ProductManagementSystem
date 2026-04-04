using System.ComponentModel.DataAnnotations;

namespace ProductManagementSystem.Shared.DataTransferObjects.Authentication;

public record class ChangePasswordDto
{
    [Required(ErrorMessage = "Email Address is required")]
    [EmailAddress(ErrorMessage = "Kindly provide a valid email")]
    public string Email { get; set; }
    [Required(ErrorMessage = "Password is required.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_])[A-Za-z\d\W_]{8,}$", ErrorMessage = "Password must be at least 8 characters and include uppercase, lowercase, a number, and a symbol.")]
    public string NewPassword { get; set; }
    [Required(ErrorMessage = "Confirm Password is required.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Password mismatch. Ensure password matches.")]
    public string ConfirmPassword { get; set; }
}
