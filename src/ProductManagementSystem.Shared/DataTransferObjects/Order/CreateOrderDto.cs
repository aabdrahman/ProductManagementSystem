using ProductManagementSystem.Shared.DataTransferObjects.OrderLineItem;
using System.ComponentModel.DataAnnotations;

namespace ProductManagementSystem.Shared.DataTransferObjects.Order;

public record class CreateOrderDto : IValidatableObject
{
    [Required(ErrorMessage = "The User Id is a required field.")]
    [Range(1, double.MaxValue, ErrorMessage = "The User Id cannot be less than 1")]
    public int UserId { get; set; }
    //[Required(ErrorMessage = "Product is a required field.")]
    //public int ProductId { get; set; }
    [Required(ErrorMessage = "Email is a required field.")]
    [EmailAddress(ErrorMessage = "Kindly provide a valid email address.")]
    public string CreatedBy { get; set; }

    [Required(ErrorMessage = "Delivery Address is a required field.")]
    [StringLength(255, ErrorMessage = "Delivery Address cannot exceed 255 characters")]
    public string DeliveryAddress { get; set; }

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Enter a valid phone number.")]
    public string PhoneNumber { get; set; }
    [Required(ErrorMessage = "Full Name is required.")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Order Line Items is required.")]
    [MinLength(1, ErrorMessage = "Order must contain at least one item.")]
    public List<CreateOrderLineItemDto> OrderLineItems { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if(string.IsNullOrEmpty(CreatedBy) || CreatedBy.Length > 100 || CreatedBy.Length < 2)
        {
            yield return new ValidationResult("Created By is a required field and must be between 2 and 100 characters.");
        }

        //if(QuantityOrdered <= 0)
        //{
        //    yield return new ValidationResult("Quantity Order cannot be less than 1.");
        //}

        //if (ProductId <= 0)
        //{
        //    yield return new ValidationResult("Product Id cannot be less than 1.");
        //}
    }
}
