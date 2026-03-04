using System.ComponentModel.DataAnnotations;

namespace ProductManagementSystem.Shared.DataTransferObjects.OrderLineItem;

public record class CreateOrderLineItemDto
{
    [Required(ErrorMessage = "The Quantity To Order is a required field.")]
    [Range(1, double.MaxValue, ErrorMessage = "The Quantity To Order cannot be less than 1")]
    public int QuantityOrdered { get; set; }
    [Required(ErrorMessage = "Product is a required field.")]
    public int ProductId { get; set; }
}