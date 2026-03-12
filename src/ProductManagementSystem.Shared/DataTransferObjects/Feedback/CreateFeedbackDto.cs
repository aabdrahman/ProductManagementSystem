using System.ComponentModel.DataAnnotations;

namespace ProductManagementSystem.Shared.DataTransferObjects.Feedback;

public record class CreateFeedbackDto
{
    public int? UserId { get; set; }
    [Required(ErrorMessage = "Message is a required field.")]
    [StringLength(1000, ErrorMessage = "Message cannot exceed 1000 characters")]
    public string Message { get; set; }
    [Required(ErrorMessage = "Name is a required field")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; }
    [Required(ErrorMessage = "User Email Address is a required field.")]
    [EmailAddress(ErrorMessage = "Kindly enter a valid email address.")]
    public string UserEmail { get; set; }
}
