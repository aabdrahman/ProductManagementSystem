using System.ComponentModel.DataAnnotations;

namespace ProductManagementSystem.Shared.DataTransferObjects.ContentDetails;

public record class CreateWhyChooseUsDto
{
    [Required(ErrorMessage = "Title is a required field.")]
    [StringLength(50, ErrorMessage = "Title cannot exceed 50 characters")]
    public string Title { get; set; }
    [Required(ErrorMessage = "Content Details is a required field.")]
    [StringLength(100, ErrorMessage = "Content details cannot exceed 100 characters")]
    public string ContentDetails { get; set; }
}


public record class UpdateWhyChooseUsDto
{
    [Required(ErrorMessage = "Id is required.")]
    public Guid Id { get; set; }
    [Required(ErrorMessage = "Title is a required field.")]
    [StringLength(50, ErrorMessage = "Title cannot exceed 50 characters")]
    public string Title { get; set; }
    [Required(ErrorMessage = "Content Details is a required field.")]
    [StringLength(100, ErrorMessage = "Content details cannot exceed 100 characters")]
    public string ContentDetails { get; set; }
}