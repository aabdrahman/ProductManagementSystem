using System.ComponentModel.DataAnnotations;

namespace ProductManagementSystem.Shared.DataTransferObjects.Order;

public record class UpdateOrderDto : IValidatableObject
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Delivery Address is a required field.")]
    [StringLength(255, ErrorMessage = "Delivery Address cannot exceed 255 characters")]
    public string DeliveryAddress { get; set; }
    //public int QuantityOrdered { get; set; }
    //public int ProductId { get; set; }
    //public string CreatedBy { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        //if (string.IsNullOrEmpty(CreatedBy) || CreatedBy.Length > 100 || CreatedBy.Length < 2)
        //{
        //    yield return new ValidationResult("Created By is a required field and must be between 2 and 100 characters.");
        //}

        //if (QuantityOrdered <= 0)
        //{
        //    yield return new ValidationResult("Quantity Order cannot be less than 1.");
        //}

        //if (ProductId <= 0)
        //{
        //    yield return new ValidationResult("Product Id cannot be less than 1.");
        //}

        if (Id <= 0)
        {
            yield return new ValidationResult("Id cannot be less than 1.");
        }
    }
}
