using System.ComponentModel.DataAnnotations;

namespace ProductManagementSystem.Shared.DataTransferObjects.User;

public record class UpdateUserConfimationStatusDto
{
    [Required(ErrorMessage = "Id is a required field.")]
    [Range(1, double.MaxValue, ErrorMessage = "Kindly provide a valid Id.")]
    public int Id { get; set; }
    [Required(ErrorMessage = "User Email Address is required.")]
    [EmailAddress]
    public string UserEmailAddress { get; set; }
}