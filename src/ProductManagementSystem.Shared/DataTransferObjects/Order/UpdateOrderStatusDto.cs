using System.ComponentModel.DataAnnotations;

namespace ProductManagementSystem.Shared.DataTransferObjects.Order;

public record class UpdateOrderStatusDto : IValidatableObject
{
    [Required(ErrorMessage = "Id is required")]
    public int Id { get; set; }
    [Required(ErrorMessage = "Kindly provide status to update.")]
    public string UpdatedStatus { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if(Id <= 0)
        {
            yield return new ValidationResult("Id cannot be less than 1");
        }

        if(!Enum.TryParse(enumType:typeof(OrderStatus), UpdatedStatus, ignoreCase: true, out var result))
        {
            yield return new ValidationResult("Invalid Order Status Provided");
        }
    }
}
