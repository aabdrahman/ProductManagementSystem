using System.ComponentModel.DataAnnotations;

namespace ProductManagementSystem.Shared.DataTransferObjects.Review;

public record class CreateReviewDto
{
    [Required(ErrorMessage = "Reviewer Name is a required field.")]
    [StringLength(100, ErrorMessage = "Reviewer Name cannot exceed 100 characters")]
    public string ReviewerName { get; set; }
    [Required(ErrorMessage = "Review is a required field.")]
    [StringLength(250, ErrorMessage = "Review cannot exceed 250 characters")]
    public string Review { get; set; }
    [StringLength(250, ErrorMessage = "Review Product Name cannot exceed 100 characters")]
    public string? ProductName { get; set; }
    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }
}