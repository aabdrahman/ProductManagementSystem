using System.ComponentModel.DataAnnotations;

namespace ProductManagementSystem.Shared.DataTransferObjects.User;

public record class UpdateUserDto
{
    [Required(ErrorMessage = "Id is a required field.")]
    [Range(1, double.MaxValue, ErrorMessage = "Kindly provide a valid Id.")]
    public int Id { get; set; }
    [Required(ErrorMessage = "User Email Address is required.")]
    [EmailAddress]
    public string UserEmailAddress { get; set; }
    [Required(ErrorMessage = "Kindly provide your address")]
    [StringLength(255, ErrorMessage = "Address should not exceed 255 characters")]
    public string Address { get; set; }
    [Required(ErrorMessage = "First Name is required.")]
    [StringLength(100, ErrorMessage = "First Name cannot exceed 100 characters.")]
    public string FirstName { get; set; }
    [Required(ErrorMessage = "Last Name is required.")]
    [StringLength(100, ErrorMessage = "First Name cannot exceed 100 characters.")]
    public string LastName { get; set; }
    [StringLength(100, ErrorMessage = "First Name cannot exceed 100 characters.")]
    public string? MiddleName { get; set; }
    [Required(ErrorMessage = "PhoneNumber is required.")]
    [StringLength(50, ErrorMessage = "First Name cannot exceed 50 characters.")]
    [Phone(ErrorMessage = "Kindly provide a valid phone number.")]
    public string PhoneNumber { get; set; }
    [Required(ErrorMessage = "Role is a required field.")]
    [Range(1, double.MaxValue, ErrorMessage = "Kindly provide a valid role.")]
    public int RoleId { get; set; }
}
