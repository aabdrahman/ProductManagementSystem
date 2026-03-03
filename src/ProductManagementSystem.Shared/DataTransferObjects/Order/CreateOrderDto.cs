using System.ComponentModel.DataAnnotations;

namespace ProductManagementSystem.Shared.DataTransferObjects.Order;

public record class CreateOrderDto : IValidatableObject
{
    [Required(ErrorMessage = "The Quantity To Order is a required field.")]
    [Range(1, double.MaxValue, ErrorMessage = "The Quantity To Order cannot be less than 1")]
    public int QuantityOrdered { get; set; }
    [Required(ErrorMessage = "Product is a required field.")]
    public int ProductId { get; set; }
    [Required(ErrorMessage = "Email is a required field.")]
    [EmailAddress(ErrorMessage = "Kindly provide a valid email address.")]
    public string CreatedBy { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if(string.IsNullOrEmpty(CreatedBy) || CreatedBy.Length > 100 || CreatedBy.Length < 2)
        {
            yield return new ValidationResult("Created By is a required field and must be between 2 and 100 characters.");
        }

        if(QuantityOrdered <= 0)
        {
            yield return new ValidationResult("Quantity Order cannot be less than 1.");
        }

        if (ProductId <= 0)
        {
            yield return new ValidationResult("Product Id cannot be less than 1.");
        }
    }
}
